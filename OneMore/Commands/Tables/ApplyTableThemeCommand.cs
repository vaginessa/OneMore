﻿//************************************************************************************************
// Copyright © 2022 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using River.OneMoreAddIn.Models;
	using System.Drawing;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Xml.Linq;
	using Resx = River.OneMoreAddIn.Properties.Resources;


	internal class ApplyTableThemeCommand : Command
	{
		public ApplyTableThemeCommand()
		{
		}


		public override async Task Execute(params object[] args)
		{
			var selectedIndex = (int)args[0];
			logger.WriteLine($"apply table theme ({selectedIndex})...");

			using var one = new OneNote(out var page, out var ns);
			if (!page.ConfirmBodyContext())
			{
				UIHelper.ShowInfo(one.Window, "Set cursor in a table"); // Resx.ApplyTableTheme_SelectTable);
				return;
			}

			// Find first selected cell as anchor point to locate table to stylize; by filtering
			// on selected=all, we avoid including the parent table of a selected nested table

			var anchor = page.Root.Descendants(ns + "Cell")
				// first dive down to find the selected T
				.Elements(ns + "OEChildren").Elements(ns + "OE")
				.Elements(ns + "T")
				.Where(e => e.Attribute("selected")?.Value == "all")
				// now move back up to the Cell
				.Select(e => e.Parent.Parent.Parent)
				.FirstOrDefault();

			if (anchor == null)
			{
				UIHelper.ShowInfo(one.Window, "Set cursor in a table"); // Resx.ApplyTableTheme_SelectTable);
				return;
			}

			var provider = new TableThemeProvider();
			if (selectedIndex < 0 || selectedIndex >= provider.Count)
			{
				UIHelper.ShowInfo(one.Window, "Invalid theme index"); // Resx.ApplyTableTheme_SelectTable);
				return;
			}

			var theme = provider.GetTheme(selectedIndex);
			var table = new Table(anchor.FirstAncestor(ns + "Table"));

			FillTable(table, theme);
			HighlightTable(table, theme);

			await one.Update(page);
		}


		private void FillTable(Table table, TableTheme theme)
		{
			if (theme.WholeTable == TableTheme.Rainbow)
			{
				if (theme.FirstColumn == TableTheme.Rainbow)
				{
					for (var r = 0; r < table.RowCount; r++)
					{
						for (var c = 0; c < table.ColumnCount; c++)
						{
							table[r][c].ShadingColor = 
								TableTheme.LightColorNames[r % TableTheme.LightColorNames.Length];
						}
					}

					return;
				}

				if (theme.HeaderRow == TableTheme.Rainbow)
				{
					for (var c = 0; c < table.ColumnCount; c++)
					{
						for (var r = 0; r < table.RowCount; r++)
						{
							table[r][c].ShadingColor =
								TableTheme.LightColorNames[c % TableTheme.LightColorNames.Length];
						}
					}

					return;
				}
			}

			string c0 = null; // even
			string c1 = null; // odd
			bool rows = true;

			if (theme.FirstRowStripe != Color.Empty && theme.SecondRowStripe != Color.Empty)
			{
				c0 = theme.FirstRowStripe.ToRGBHtml();
				c1 = theme.SecondRowStripe.ToRGBHtml();
			}
			else if (theme.FirstColumnStripe != Color.Empty && theme.SecondColumnStripe != Color.Empty)
			{
				c0 = theme.FirstColumnStripe.ToRGBHtml();
				c1 = theme.SecondColumnStripe.ToRGBHtml();
				rows = false;
			}
			else if (theme.WholeTable != Color.Empty)
			{
				c0 = c1 = theme.WholeTable.ToRGBHtml();
			}

			if (!string.IsNullOrEmpty(c0))
			{
				for (var c = 0; c < table.ColumnCount; c++)
				{
					for (var r = 0; r < table.RowCount; r++)
					{
						table[r][c].ShadingColor = (rows ? r : c) % 2 == 0 ? c0 : c1;
					}
				}
			}
		}


		private void HighlightTable(Table table, TableTheme theme)
		{
			if (theme.FirstColumn == TableTheme.Rainbow)
			{
				for (int r = 0; r < table.RowCount; r++)
				{
					table[r][0].ShadingColor = TableTheme.MediumColorNames[r % TableTheme.MediumColorNames.Length];
				}
			}
			else if (theme.FirstColumn != Color.Empty)
			{
				var color = theme.FirstColumn.ToRGBHtml();
				for (int r = 0; r < table.RowCount; r++)
				{
					table[r][0].ShadingColor = color;
				}
			}

			if (theme.LastColumn != Color.Empty)
			{
				var color = theme.LastColumn.ToRGBHtml();
				for (int r = 0; r < table.RowCount; r++)
				{
					table[r][table.ColumnCount - 1].ShadingColor = color;
				}
			}

			if (theme.HeaderRow == TableTheme.Rainbow)
			{
				for (int c = 0; c < table.ColumnCount; c++)
				{
					table[0][c].ShadingColor = TableTheme.MediumColorNames[c % TableTheme.MediumColorNames.Length];
				}
			}
			else if (theme.HeaderRow != Color.Empty)
			{
				var color = theme.HeaderRow.ToRGBHtml();
				for (int c = 0; c < table.ColumnCount; c++)
				{
					table[0][c].ShadingColor = color;
				}
			}

			if (theme.TotalRow != Color.Empty)
			{
				var color = theme.TotalRow.ToRGBHtml();
				for (int c = 0; c < table.ColumnCount; c++)
				{
					table[table.RowCount - 1][c].ShadingColor = color;
				}
			}

			if (theme.HeaderFirstCell != Color.Empty)
			{
				table[0][0].ShadingColor = theme.HeaderFirstCell.ToRGBHtml();
			}

			if (theme.HeaderLastCell != Color.Empty)
			{
				table[0][table.ColumnCount - 1].ShadingColor = theme.HeaderLastCell.ToRGBHtml();
			}

			if (theme.TotalFirstCell != Color.Empty)
			{
				table[table.RowCount - 1][0].ShadingColor = theme.TotalFirstCell.ToRGBHtml();
			}

			if (theme.TotalLastCell != Color.Empty)
			{
				table[table.RowCount - 1][table.ColumnCount - 1].ShadingColor = theme.TotalLastCell.ToRGBHtml();
			}
		}
	}
}
