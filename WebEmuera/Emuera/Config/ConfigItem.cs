﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using MinorShift.Emuera.Sub;
using EvilMask.Emuera;

namespace MinorShift.Emuera
{

	internal abstract class AConfigItem
	{
		#region EM_私家版_Emuera多言語化改造
		// public AConfigItem(ConfigCode code, string text)
		public AConfigItem(ConfigCode code, string text, string etext)
		{
			this.Code = code;
			this.Name = code.ToString();
			// this.Text = text;
			this.Text = text.ToUpper();
			// this.EngText = etext;
			this.EngText = etext.ToUpper();
		}
		#endregion

		#region EM_私家版_Emuera多言語化改造
		public static ConfigItem<T> Copy<T>(ConfigItem<T> other)
		{
			if(other == null)
				return null;
			//ConfigItem<T> ret = new ConfigItem<T>(other.Code, other.Text, other.Value);
			ConfigItem<T> ret = new ConfigItem<T>(other.Code, other.Text, other.EngText, other.Value);
			ret.Fixed = other.Fixed;
			return ret;
		}
		#endregion

		public abstract void CopyTo(AConfigItem other);
		public abstract bool TryParse(string tokens);
		public abstract void SetValue<U>(U p);
		public abstract U GetValue<U>();
		public abstract string ValueToString();
		public readonly ConfigCode Code;
		public readonly string Name;
		public readonly string Text;
		#region EM_私家版_Emuera多言語化改造
		public readonly string EngText;
		#endregion
		public bool Fixed;
	}
	
	internal sealed class ConfigItem<T> : AConfigItem
	{
		#region EM_私家版_Emuera多言語化改造
		public ConfigItem(ConfigCode code,string text, string etext, T t):base(code, text, etext)
		#endregion
		{
			this.val = t;
		}
		private T val;
		public T Value
		{
			get{return val;}
			set
			{
				if(Fixed)
					return;
				val = value;
			}
		}

		public override void CopyTo(AConfigItem other)
		{

			ConfigItem<T> item = ((ConfigItem<T>)other);
			item.Fixed = false;
			item.Value = this.Value;
			item.Fixed = this.Fixed;
		}

		public override void SetValue<U>(U p)
		{
			//if (this is ConfigItem<U>)
				((ConfigItem<U>)(AConfigItem)this).Value = p;
            //else
            //    throw new ExeEE("型が一致しない");
		}

		public override U GetValue<U>()
		{
            ////if (this is ConfigItem<U>)
				return ((ConfigItem<U>)(AConfigItem)this).Value;
			//throw new ExeEE("型が一致しない");
		}

		public override string ValueToString()
		{
			if(this is ConfigItem<bool>)
			{
				//ConfigItem<T>をConfigItem<bool>に直接キャストすることはできない
				bool b = ((ConfigItem<bool>)(AConfigItem)this).Value;
				if (b)
					return "YES";
				return "NO";
			}
			if (this is ConfigItem<Color>)
			{
				Color c = ((ConfigItem<Color>)(AConfigItem)this).Value;
				return string.Format("{0},{1},{2}", c.R, c.G, c.B);
			}

			#region EM_私家版_LoadText＆SaveText機能拡張
			if (this is ConfigItem<List<string>>)
			{
				var sb = new StringBuilder();
				var v = ((ConfigItem<List<string>>)(AConfigItem)this).Value;
				foreach (var str in v)
				{
					if (sb.Length > 0)
						sb.Append(",");
					sb.Append(str);
				}
				return sb.ToString();
			}
			#endregion
			return val.ToString();
		}
		
		
		public override string ToString()
		{
			#region EM_私家版_Emuera多言語化改造
			return (Config.EnglishConfigOutput ? EngText : Text) + ":" + ValueToString();
			#endregion
		}



		/// ジェネリック化大失敗。なんかうまい方法ないかな～
		public override bool TryParse(string param)
		{
			bool ret = false;
			if ((param == null) || (param.Length == 0))
				return false;
			if(this.Fixed)
				return false;
			string str = param.Trim();
			if (this is ConfigItem<bool>)
			{
				bool b = false;
				ret = tryStringToBool(str, ref b);
				if (ret)//ConfigItem<T>をConfigItem<bool>に直接キャストすることはできない
					((ConfigItem<bool>)(AConfigItem)this).Value = b;
			}
			else if (this is ConfigItem<Color>)
			{
				Color c;
				ret = tryStringsToColor(str, out c);
				if (ret)
					((ConfigItem<Color>)(AConfigItem)this).Value = c;
                else
                    throw new CodeEE(Lang.Error.NotExistColorSpecifier.Text);
            }
			else if (this is ConfigItem<char>)
			{
				char c;
				ret = char.TryParse(str, out c);
				if (ret)
					((ConfigItem<char>)(AConfigItem)this).Value = c;
			}
			else if (this is ConfigItem<Int32>)
			{
				Int32 i;
				ret = Int32.TryParse(str, out i);
				if (ret)
					((ConfigItem<Int32>)(AConfigItem)this).Value = i;
                else
                    throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
            }
			else if (this is ConfigItem<Int64>)
			{
				Int64 i;
				ret = Int64.TryParse(str, out i);
                if (ret)
                    ((ConfigItem<Int64>)(AConfigItem)this).Value = i;
                else
                    throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
			}
            else if (this is ConfigItem<List<Int64>>)
            {
                ((ConfigItem<List<Int64>>)(AConfigItem)this).Value.Clear();
                Int64 i;
                string[] strs = str.Split('/');
                foreach (string st in strs)
                {
                    ret = Int64.TryParse(st.Trim(), out i);
                    if (ret)
                        ((ConfigItem<List<Int64>>)(AConfigItem)this).Value.Add(i);
                    else
                    {
                        throw new CodeEE(Lang.Error.ContainsNonNumericCharacters.Text);
                    }
                }
            }
            else if (this is ConfigItem<string>)
            {
                ret = true;
                ((ConfigItem<string>)(AConfigItem)this).Value = str;
            }
            else if (this is ConfigItem<List<string>>)
            {
				#region EM_私家版_LoadText＆SaveText機能拡張
				// ret = true;
				// ((ConfigItem<List<string>>)(AConfigItem)this).Value.Add(str);
				ret = true;
				var list = ((ConfigItem<List<string>>)(AConfigItem)this).Value;
				tryStringToStringList(str, ref list);
				((ConfigItem<List<string>>)(AConfigItem)this).Value = list;
				#endregion
			}
			else if (this is ConfigItem<TextDrawingMode>)
            {
                str = str.ToUpper();
                ret = Enum.IsDefined(typeof(TextDrawingMode), str);
                if (ret)
                {
                    ((ConfigItem<TextDrawingMode>)(AConfigItem)this).Value
                     = (TextDrawingMode)Enum.Parse(typeof(TextDrawingMode), str);
                }
                else
                    throw new CodeEE(Lang.Error.InvalidSpecification.Text);
            }
            else if (this is ConfigItem<ReduceArgumentOnLoadFlag>)
            {
                str = str.ToUpper();
                ret = Enum.IsDefined(typeof(ReduceArgumentOnLoadFlag), str);
                if (ret)
                {
                    ((ConfigItem<ReduceArgumentOnLoadFlag>)(AConfigItem)this).Value
                     = (ReduceArgumentOnLoadFlag)Enum.Parse(typeof(ReduceArgumentOnLoadFlag), str);
                }
                else
                    throw new CodeEE(Lang.Error.InvalidSpecification.Text);
            }
            else if (this is ConfigItem<DisplayWarningFlag>)
            {
                str = str.ToUpper();
                ret = Enum.IsDefined(typeof(DisplayWarningFlag), str);
                if (ret)
                {
                    ((ConfigItem<DisplayWarningFlag>)(AConfigItem)this).Value
                     = (DisplayWarningFlag)Enum.Parse(typeof(DisplayWarningFlag), str);
                }
                else
                    throw new CodeEE(Lang.Error.InvalidSpecification.Text);
            }
            else if (this is ConfigItem<UseLanguage>)
            {
                str = str.ToUpper();
                ret = Enum.IsDefined(typeof(UseLanguage), str);
                if (ret)
                {
                    ((ConfigItem<UseLanguage>)(AConfigItem)this).Value
                        = (UseLanguage)Enum.Parse(typeof(UseLanguage), str);
                }
                else
                    throw new CodeEE(Lang.Error.InvalidSpecification.Text);
            }
            else if (this is ConfigItem<TextEditorType>)
            {
                str = str.ToUpper();
                ret = Enum.IsDefined(typeof(TextEditorType), str);
                if (ret)
                {
                    ((ConfigItem<TextEditorType>)(AConfigItem)this).Value
                        = (TextEditorType)Enum.Parse(typeof(TextEditorType), str);
                }
                else
                    throw new CodeEE(Lang.Error.InvalidSpecification.Text);
            }
            //else
            //    throw new ExeEE("型不明なコンフィグ");
			return ret;
		}


		#region EM_私家版_LoadText＆SaveText機能拡張
		private bool tryStringToStringList(string arg, ref List<string> vs)
		{
			string[] tokens = arg.Split(',');
			vs.Clear();
			foreach (var token in tokens)
			{
				vs.Add(token.Trim());
			}
			return true;
		}
		#endregion

		private bool tryStringToBool(string arg, ref bool p)
		{
			if (arg == null)
				return false;
			string str = arg.Trim();
			if (Int32.TryParse(str, out int i))
			{
				p = (i != 0);
				return true;
			}
			if (str.Equals("NO", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("FALSE", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("後", StringComparison.CurrentCultureIgnoreCase))//"単位の位置"用
			{
				p = false;
				return true;
			}
			if (str.Equals("YES", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("TRUE", StringComparison.CurrentCultureIgnoreCase)
				|| str.Equals("前", StringComparison.CurrentCultureIgnoreCase))
			{
				p = true;
				return true;
			}
			throw new CodeEE(Lang.Error.InvalidSpecification.Text);
		}

		private bool tryStringsToColor(string str, out Color c)
		{
			string[] tokens = str.Split(',');
			c = Color.Black;
			int r, g, b;
			if (tokens.Length < 3)
				return false;
			if (!Int32.TryParse(tokens[0].Trim(), out r) || (r < 0) || (r > 255))
				return false;
			if (!Int32.TryParse(tokens[1].Trim(), out g) || (g < 0) || (g > 255))
				return false;
			if (!Int32.TryParse(tokens[2].Trim(), out b) || (b < 0) || (b > 255))
				return false;
			c = Color.FromArgb(r, g, b);
			return true;
		}
	}
}
