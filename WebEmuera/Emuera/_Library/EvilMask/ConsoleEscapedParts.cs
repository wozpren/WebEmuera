using MinorShift.Emuera;
using MinorShift.Emuera.GameView;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvilMask.Emuera
{
	internal sealed class ConsoleEscapedParts
	{
		const int ROW_LINE = 0, ROW_DEPTH = 1, ROW_TOP = 2, ROW_BOTTOM = 3, ROW_ID = 4, ROW_DIVTYPE = 5;
		static readonly DataTable dt = new DataTable(); // divの情報を保存する場所
		static readonly Dictionary<Int64, AConsoleDisplayPart> parts = new Dictionary<long, AConsoleDisplayPart>(); //　実際にdivデータを保存する場所
		/// <summary>
		/// 最近GetPartsInRangeを行ったか
		/// </summary>
		static bool getOnce = false;
		/// <summary>
		/// 前回GetPartsInRangeを行った時の状態
		/// </summary>
		static int lastTop, lastBottom, lastGeneration;
		/// <summary>
		/// 前回のGetPartsInRangeから変動があるか
		/// </summary>
		public static bool Changed { get; private set; }
		/// <summary>
		/// 前回GetPartsInRangeを行った時と状況が変わったらfalseを返す。
		/// 重複計算防止用。
		/// </summary>
		public static bool TestedInRange(int top, int bottom, int gen)
		{
			return getOnce && top == lastTop && bottom == lastBottom && gen == lastGeneration;
		}
		static ConsoleEscapedParts()
		{
			// データベースを構築
			dt.Columns.Add("line", typeof(int)); // div所属のline
			dt.Columns.Add("depth", typeof(int)); // divの奥行き
			dt.Columns.Add("top", typeof(int)); // divの一行目がこの行より下回る
			dt.Columns.Add("bottom", typeof(int)); // divの一行目がこの行より上回る
			dt.Columns.Add("id", typeof(Int64));　// ユニークID（保存時点のTimeStamp）
			dt.Columns.Add("div", typeof(sbyte)); //　divの種類（display:absoluteかdisplay:relativeか）
		}
		public static void Clear()
		{
			getOnce = false;
			dt.Clear();
			parts.Clear();
			Changed = false;
		}
		public static void Add(AConsoleDisplayPart part, int line, int depth, int top, int bottom)
		{
			var id = Utils.TimePoint();
			var row = dt.NewRow();
			row[ROW_LINE] = line;
			row[ROW_DEPTH] = depth;
			row[ROW_TOP] = top;
			row[ROW_BOTTOM] = bottom;
			row[ROW_ID] = id;
			if (part is ConsoleDivPart div)
				row[ROW_DIVTYPE] = (!div.IsRelative ? 2 : 0) | 1;　// bit1 : divであるなら1　bit2 : divがdisplay:absoluteなら1
			else
				row[ROW_DIVTYPE] = 0;
			dt.Rows.Add(row);
			parts.Add(id, part);
			Changed = true;
		}
		/// <summary>
		/// 所在行のlineNoがlineの行とその以降の行のdivデータを全部削除
		/// </summary>
		public static void Remove(int line)
		{
			foreach (var row in DataTableExtensions.AsEnumerable(dt).Where(r => (int)r[ROW_LINE] >= line).ToArray())
			{
				parts.Remove((Int64)row[ROW_ID]);
				dt.Rows.Remove(row);
				Changed = true;
			}
		}
		/// <summary>
		/// 所在行のlineNoがlineの行のdivデータを削除
		/// </summary>
		public static void RemoveAt(int line)
		{
			foreach (var row in DataTableExtensions.AsEnumerable(dt).Where(r => (int)r[ROW_LINE] == line).ToArray())
			{
				parts.Remove((Int64)row[ROW_ID]);
				dt.Rows.Remove(row);
				Changed = true;
			}
		}
		/// <summary>
		/// top行からbottom行までの範囲内表示内容があったdivをrmapで保存。
		/// genは今のbuttonGeneration，重複計算防止用。
		/// </summary>
		public static void GetPartsInRange(int top, int bottom, int gen, Dictionary<int, List<AConsoleDisplayPart>> rmap)
		{
			if (GlobalStatic.Console?.GetLineNo > Config.MaxLog)
			{
				var correction = GlobalStatic.Console.GetLineNo - Config.MaxLog;
				top += correction;
				bottom += correction;
			}
			if (rmap == null) return;
			rmap.Clear();
			// すべてのdisplay:absoluteのdiv
			// とtop行からbottom行までの範囲内表示内容があったdiv
			// と所在行がtop行からbottom行までの範囲内のdiv
			// に対し
			foreach (var row in DataTableExtensions.AsEnumerable(dt)
				.Where(r => ((sbyte)r[ROW_DIVTYPE] & 2) != 0 || ((int)r[ROW_TOP] <= bottom + 1 && (int)r[ROW_BOTTOM] >= top && r[ROW_LINE] is int line
				&& ((sbyte)r[ROW_DIVTYPE] != 0 || top > line || line > bottom + 1))))
			{
				List<AConsoleDisplayPart> list = null;
				rmap.TryGetValue((int)row[ROW_DEPTH], out list);
				if (list == null)
				{
					list = new List<AConsoleDisplayPart>();
					rmap.Add((int)row[ROW_DEPTH], list);
				}
				list.Add(parts[(Int64)row[ROW_ID]]);
			}
			getOnce = true;
			lastTop = top; lastBottom = bottom; lastGeneration = gen;
			Changed = false;
		}
	}
}
