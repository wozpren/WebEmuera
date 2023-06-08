using EvilMask.Emuera;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EvilMask.Emuera.Shape;
using static EvilMask.Emuera.Utils;

namespace MinorShift.Emuera.GameView
{
	class ConsoleDivPart : AConsoleDisplayPart
	{
		public ConsoleDivPart(MixedNum xPos, MixedNum yPos, MixedNum width, MixedNum height, int depth, int color, StyledBoxModel box, bool isRelative, ConsoleDisplayLine[] childs)
		{
			backgroundColor = color >= 0 ? Color.FromArgb((int)(color | 0xff000000)) : Color.Transparent;
			StringBuilder sb = new StringBuilder();
			width.num = Math.Abs(width.num);
			height.num = Math.Abs(height.num);
			sb.Append("<div");
			Utils.AddTagMixedNumArg(sb, "xpos", xPos);
			Utils.AddTagMixedNumArg(sb, "ypos", yPos);
			Utils.AddTagMixedNumArg(sb, "width", width);
			Utils.AddColorParam(sb, "color", backgroundColor);
			Utils.AddTagMixedNumArg(sb, "height", height);
			if (box != null)
			{
				Utils.AddTagMixedParam(sb, "margin", box.margin);
				Utils.MixedNum4ToInt4(box.margin, ref margin);
				Utils.AddTagMixedParam(sb, "padding", box.padding);
				Utils.MixedNum4ToInt4(box.padding, ref padding);
				Utils.AddTagMixedParam(sb, "border", box.border);
				Utils.MixedNum4ToInt4(box.border, ref border);
				Utils.AddTagMixedParam(sb, "radius", box.radius);
				Utils.MixedNum4ToInt4(box.radius, ref radius);
				if (box.color != null)
				{
					borderColors = new Color[4];
					for (int i = 0; i < 4; i++)
						borderColors[i] = box.color[i] >= 0 ? Color.FromArgb((int)(box.color[i] | 0xff000000)) : Color.Transparent;
					Utils.AddColorParam4(sb, "bcolor", borderColors);
				}
			}
			sb.Append(">");
			altHeadTag = sb.ToString();
			Str = string.Empty;
			xOffset = MixedNum.ToPixel(xPos, 0);
			#region EE_div各要素の修正
			if (margin != null)	divXOffset += margin[Direction.Left];
			if (padding != null) divXOffset += padding[Direction.Left];
			if (border != null) divXOffset += border[Direction.Left];
			#endregion
			PointY = MixedNum.ToPixel(yPos, 0);

			#region EE_div各要素の修正
			if (margin != null) yOffset += margin[Direction.Top];
			if (padding != null) yOffset += padding[Direction.Top];
			if (border != null) yOffset += border[Direction.Top];
			#endregion

			this.width = MixedNum.ToPixel(width, 0);
			Height = MixedNum.ToPixel(height, 0);
			children = childs;
			Depth = depth;
			IsRelative = isRelative;
		}
		int pointX = 0;
		int xOffset;
		#region EE_div各要素の修正
		int divXOffset;
		int yOffset;
		#endregion
		int width;
		public override int PointX {
			get { return pointX; }
			set { pointX = value;
				foreach (var child in children)
					#region EE_div各要素の修正
					child.ShiftPositionX(value + xOffset + divXOffset);
					#endregion
			} }
		int PointY;
		int Height;
		int[] margin, padding, radius, border;
		Color[] borderColors;
		Color backgroundColor;
		string altHeadTag;
		readonly ConsoleDisplayLine[] children;
		public bool IsEscaped { get; set; } = false;
		public override int Top { get { return PointY; } }
		public override int Bottom { get { return PointY + Height; } }
		public bool IsRelative { get; private set; }
		public ConsoleDisplayLine[] Children { get { return children; } }

		public override bool CanDivide => false;
		public ConsoleButtonString TestChildHitbox(int pointX, int pointY, int relPointY)
		{
			ConsoleButtonString pointing = null;
			#region EE_div各要素の修正
			var rect = new Rectangle(PointX + xOffset, relPointY + PointY + yOffset, width, Height);
			#endregion
			if (!rect.Contains(pointX, pointY)) return null;
			relPointY = rect.Y;
			foreach (var line in children)
			{
				for (int b = 0; b < line.Buttons.Length; b++)
				{
					ConsoleButtonString button = line.Buttons[line.Buttons.Length - b - 1];
					if (button == null || button.StrArray == null)
						continue;
					if ((button.PointX <= pointX) && (button.PointX + button.Width >= pointX))
					{
						//if (relPointY >= 0 && relPointY <= Config.FontSize)
						//{
						//	pointing = button;
						//	if(pointing.IsButton)
						//		goto breakfor;
						//}
						foreach (AConsoleDisplayPart part in button.StrArray)
						{
							if (part == null)
								continue;
							if ((part.PointX <= pointX) && (part.PointX + part.Width >= pointX)
								&& (relPointY + part.Top <= pointY) && (relPointY + part.Bottom >= pointY))
							{
								pointing = button;
								if (pointing.IsButton)
									return pointing;
							}
						}
					}
				}
				relPointY += Config.LineHeight;
			}
			return pointing;
		}
		public override void DrawTo(Graphics graph, int pointY, bool isSelecting, bool isBackLog, TextDrawingMode mode)
		{
			if (GlobalStatic.MainWindow == null) return;
			var rect = IsRelative ? new Rectangle(PointX + xOffset, pointY + PointY, width, Height) 
				: new Rectangle(xOffset, GlobalStatic.MainWindow.MainPicBox.Height - PointY - Height, width, Height);

			if (margin != null)
				rect = new Rectangle(rect.X + margin[Direction.Left], rect.Y + margin[Direction.Top],
					 rect.Width - margin[Direction.Left] - margin[Direction.Right], rect.Height - margin[Direction.Top] - margin[Direction.Bottom]);
			graph.SetClip(rect, CombineMode.Replace);

			Shape.BoxBorder.DrawBorder(graph, rect, border, radius, borderColors, backgroundColor);

			if (border != null)
				rect = new Rectangle(rect.X + border[Direction.Left], rect.Y + border[Direction.Top],
					 rect.Width - border[Direction.Left] - border[Direction.Right], rect.Height - border[Direction.Top] - border[Direction.Bottom]);

			if (padding != null)
				rect = new Rectangle(rect.X + padding[Direction.Left], rect.Y + padding[Direction.Top],
					 rect.Width - padding[Direction.Left] - padding[Direction.Right], rect.Height - padding[Direction.Top] - padding[Direction.Bottom]);

			graph.SetClip(rect, CombineMode.Replace);

			pointY = rect.Y;
			foreach (var child in children)
			{
				child.DrawTo(graph, pointY, isBackLog, true, mode);
				pointY += Config.LineHeight;
			}
			graph.ResetClip();
		}

		public override void GDIDrawTo(int pointY, bool isSelecting, bool isBackLog)
		{
			// WINAPI では使えない
		}

		public override void SetWidth(StringMeasure sm, float subPixel)
		{
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append(altHeadTag);
			foreach (var line in children)
			{
				line.BuildString(sb);
				sb.Append("\r\n");
			}
			sb.Append("</div>");
			return sb.ToString();
		}
		public override StringBuilder BuildString(StringBuilder sb)
		{
			sb.Append(altHeadTag);
			foreach (var line in children)
			{
				line.BuildString(sb);
				sb.Append("\r\n");
			}
			sb.Append("</div>");
			return sb;
		}
	}
}
