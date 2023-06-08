using MinorShift.Emuera.GameView;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinorShift.Emuera.Forms
{
	public partial class RikaiDialog : Form
	{
		private string filename;
		private byte[] edict;
		private byte[] edictind;
		public delegate void RikaiSendIndex(byte[] edictind);
		private RikaiSendIndex rikaiSendIndex;
		public Encoding eucjp = Encoding.GetEncoding(20932);
		private List<string> dialogLines = new List<string>(16);
		DateTime start = DateTime.Now;

		public RikaiDialog(string filename, byte[] edict, RikaiSendIndex rikaiSendIndex)
		{
			this.filename = filename;
			this.edict = edict;
			this.rikaiSendIndex = rikaiSendIndex;

			dialogLines.Add("");
			dialogLines.Add("");
			dialogLines.Add("");
			dialogLines.Add("");
			dialogLines.Add("");
			dialogLines.Add("");

			InitializeComponent();

			backgroundWorker.DoWork += new DoWorkEventHandler(bwDoWork);
			backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwRunWorkerCompleted);
			backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(bwProgressChanged);

			this.label.Text = "starting";

			backgroundWorker.RunWorkerAsync();
		}

		//public string LabelText
		//{
		//	get
		//	{
		//		return this.label.Text;
		//	}
		//	set
		//	{
		//		this.label.Text = value;
		//	}
		//}


		private void bwRunWorkerCompleted(
			object sender, RunWorkerCompletedEventArgs e)
		{
			//this.label.Text = "completed";

			rikaiSendIndex(edictind);
		}
		
		private void bwProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			var per = e.ProgressPercentage;
			dialogLines[0] = $"Generating {Config.RikaiFilename}.ind, {e.ProgressPercentage}% done.";
			if (per >= 20 && dialogLines[1].Length == 0)
			{
				var timeSpan = DateTime.Now - start;
				dialogLines[1] = $"20% in {(int)timeSpan.TotalSeconds} seconds";
			}
			if (per >= 40 && dialogLines[2].Length == 0)
			{
				var timeSpan = DateTime.Now - start;
				dialogLines[2] = $"40% in {(int)timeSpan.TotalSeconds} seconds";
			}
			if (per >= 60 && dialogLines[3].Length == 0)
			{
				var timeSpan = DateTime.Now - start;
				dialogLines[3] = $"60% in {(int)timeSpan.TotalSeconds} seconds";
			}
			if (per >= 80 && dialogLines[4].Length == 0)
			{
				var timeSpan = DateTime.Now - start;
				dialogLines[4] = $"80% in {(int)timeSpan.TotalSeconds} seconds";
			}
			if (per >= 100 && dialogLines[5].Length == 0)
			{
				var timeSpan = DateTime.Now - start;
				dialogLines[5] = $"100% in {(int)timeSpan.TotalSeconds} seconds";
			}
			//dialogLines.Add("Progress: 0/0");
			//dialogLines[0] = $"{Config.RikaiFilename}.ind not found, generating. Should take a few minutes.";
			//dialogLines[1] = "Progress 0/0";
			this.label.Text = string.Join("\n", dialogLines);

			//this.label.Text = "in progress: " + e.ProgressPercentage;
		}

		class EdictParser
		{
			public byte[] edict;
			public int end = 0, start = 0;
			public bool finished = false;
			public const long tickDelta = 4000;
			public long tickNext = DateTime.Now.Ticks + tickDelta;
			public byte[] word;
			public byte[] pronoun;

			private Encoding eucjp;

			public EdictParser(byte[] a_edict)
			{
				edict = a_edict; //LATER: I hope it doesn't copy the entire array, and only copies the reference.

				eucjp = Encoding.GetEncoding(20932);

				for (; end < edict.Length; ++end)
				{
					if (edict[end] == '\n')
					{
						end--;
						start = end;
						break;
					}
				}
			}

			public void Step()
			{
				word = null;
				pronoun = null;
				end += 2;
				start = end;

				if (end >= edict.Length)
				{
					this.finished = true;
					return;
				}

				for (; end < edict.Length; ++end)
				{
					if (edict[end] == '\n')
					{
						break;
					}
				}

				end--;

				int c = start;
				for (; c < end; c++)
				{
					if (edict[c] == ',')
					{
						throw new Exception("',' is possible, huh");
					}
					else if (edict[c] == '/')
					{
						int b = c;
						//if (edict[b - 1] == ' ') b--;
						b--;
						word = new byte[b - start];
						Array.Copy(edict, start, word, 0, b - start);
						break;
					}
					else if (edict[c] == '[')
					{
						int b = c;
						b--;
						word = new byte[b - start];
						Array.Copy(edict, start, word, 0, b - start);
						c++;
						int pronoun_start = c;
						for (; c < end; c++)
						{
							if (edict[c] == ']')
							{
								pronoun = new byte[c - pronoun_start];
								Array.Copy(edict, pronoun_start, pronoun, 0, c - pronoun_start);
								c = end; //superbreak
								break;
							}
							else if (edict[c] == ',')
							{
								throw new Exception("',' is possible, take 2");
							}
						}
					}
				}
				return;
			}
		}

		class IndexEntry
		{
			public byte[] word;
			public List<int> offsets;
			public IndexEntry Next = null;
			public IndexEntry Previous = null;

			public IndexEntry(byte[] a_word)
			{
				word = a_word;
				offsets = new List<int>(4);
			}

			public void InsertBetween(IndexEntry a_previous, IndexEntry a_next)
			{
				a_previous.Next = this;
				Previous = a_previous;
				a_next.Previous = this;
				Next = a_next;
			}
		}

		List<string> s(IndexEntry first)
		{
			List<string> res = new List<string>(64);
			for (int i = 0; i < 63; i++)
			{
				var t = eucjp.GetString(first.word);
				t += " -- ";
				t += BitConverter.ToString(first.word);
				res.Add(t);
				if (first.Next == null) break;
				first = first.Next;
			}
			return res;
		}

		string s(byte[] first)
		{
			var t = eucjp.GetString(first);
			t += " -- ";
			t += BitConverter.ToString(first);
			return t;
		}

		List<string> check(IndexEntry first)
		{
			while (true)
			{
				if (first.Next == null) break;
				if (Compare(first.word, first.Next.word) >= 0)
				{
					var err = new List<string>(16);
					for (int i = 0; i < 8; i++)
					{
						if (first.Previous == null) break;
					}
					for (int i = 0; i < 16; i++)
					{
						var t = eucjp.GetString(first.word);
						t += " -- ";
						t += BitConverter.ToString(first.word);
						err.Add(t);
						if (first.Next == null) break;
						first = first.Next;
					}
					return err;
				}

				first = first.Next;
			}
			return null;
		}

		//In theory one of them will always stay in the kana area, while other will be all over the place.
		//I will check if this is true or not once this thing actually works.
		int WhichIsCloser(byte[] first, byte[] second, byte[] tothis)
		{
			if (first[0] == second[0] && second[0] == tothis[0]) return 0;
			int s = second[0] - tothis[0];
			int f = first[0] - tothis[0];
			if (s < 0) s *= -1;
			if (f < 0) f *= -1;
			if (s < f) return 1; //do swap
			return 0;
		}


		private int Compare(byte[] first, byte[] second)
		{
			int i = 0;
			while (true)
			{
				if (i >= first.Length)
				{
					if (i >= second.Length) return 0;
					return -1;
				}
				if (i >= second.Length)
					return 1;

				if (first[i] > second[i]) return 1;
				if (first[i] < second[i]) return -1;
				i++;
			}
		}

		private void bwDoWork(object sender, DoWorkEventArgs e)
		{
			//GenerateIndex

			// So, let me describe how the search index looks like.
			// 0x00 search_word 0x01 offset 0x00 search_word 0x01 offset 0x00
			// search_word is the word we are searching for
			// offset is the position of this word in the dictionary
			// In case the word appears more than once, there will be multiple offsets.
			// 0x00 search_word 0x01 offset 0x01 offset 0x00
			// I can guarantee that 0x00 0x01 and 0x02 will never be used for encoding search_words, but since I can't be so sure about offsets, this is how offsets are encoded:
			// 1) Offsets are stored byte by byte, from most significant to least significant
			// 2) If there is ever a need to encode 0x00 in the offset itself, you use 0x02 ((0 << 2) + 2) instead. If you need 0x01, you use 0x02 ((1 << 2) + 2).
			// If you need 0x02, you use 0x02 ((2 << 2) + 2). If you need 0x03, you use ((3 << 2) + 2).
			// Just using plain text with '\n' and ',' as separators probably would've been simplier and only 20% bigger, but whatever, let's suffer.

			// The whole point of this is, this index is sorted, so you do binary search with byte sequence.
			// When you need to search for search_word, you search for 0x00 search_word 0x01, and it's guaranteed that there is only one such byte sequence in the index.

			BackgroundWorker worker = sender as BackgroundWorker;

			bool paranoid = false;
			if (paranoid)
			{
				for (int i = 0, iend = edict.Length; i < iend; i++)
				{
					if (edict[i] == 0 || edict[i] == 1 || edict[i] == 2)
					{
						throw new Exception("My search index works under an assumption that you will never meet 0x00 or 0x01 or 0x02 in the dictionary text");
					}
				}
			}

			////var dialog = new ConfigDialog { StartPosition = FormStartPosition.CenterParent };
			//var dialog = new RikaiDialog();
			//var dialogLines = new List<string>(16);
			//dialogLines.Add($"{Config.RikaiFilename}.ind not found, generating. Should take a few minutes.");
			//dialogLines.Add("Progress: 0/0");
			////dialogLines[0] = $"{Config.RikaiFilename}.ind not found, generating. Should take a few minutes.";
			////dialogLines[1] = "Progress 0/0";
			//dialog.LabelText = string.Join("\n", dialogLines);
			//dialog.Show();
			//MessageBox.Show($"{Config.RikaiFilename}.ind not found, generating. Should take a few minutes.");

			//List<>
			//List<IndexEntry> index = new List<IndexEntry>();
			//var f = index.First();
			IndexEntry first, current, current2;

			int oldPercentage = -1;

			var edictParser = new EdictParser(edict);

			edictParser.Step();
			first = current = current2 = new IndexEntry(edictParser.word);
			current.offsets.Add(edictParser.start);

			edictParser.Step();
			current = new IndexEntry(edictParser.word);
			current.offsets.Add(edictParser.start);
			first.Next = current;
			current.Previous = first;

			paranoid = false;
			if (paranoid)
			{
				if (Compare(first.word, current.word) > 0) //if first.word > current.word
				{
					throw new Exception($"{Config.RikaiFilename} parsing error");
				}
			}

			//int itermax = 0;
			//while (true)
			//{
			//	itermax++;
			//	edictParser.Step();
			//	if (edictParser.finished) break;

			//}
			//itermax += 0;

			int iter = 0; //breaks at 0x13
			while (true)
			{
				//if (check(first) != null)
				//{
				//	iter += 0;
				//}
				edictParser.Step();
				if (edictParser.finished) break;
				else
				{
					long tickNew = DateTime.Now.Ticks;
					if (tickNew > edictParser.tickNext)
					{
						edictParser.tickNext = tickNew + EdictParser.tickDelta;
						int percentage = (int)((float)edictParser.end / (float)edict.Length * 100);
						if (oldPercentage != percentage)
						{
							oldPercentage = percentage;
							worker.ReportProgress(percentage);
						}
					}
				}

				if (WhichIsCloser(current.word, current2.word, edictParser.word) != 0)
				{
					var temp = current;
					current = current2;
					current2 = temp;
				}

				//type s(first) in the immediate window to debug this thing

				var word = edictParser.word;
				int cmp;

				//if (iter == 0x00004a06)
				//{
				//	iter = 0x00004a06;
				//}

				//if (Compare(word, eucjp.GetBytes("わじるし")) == 0)
				//{
				//	iter = iter;
				//}



				cmp = Compare(current.word, word);
				if (cmp == 0)
				{
					current.offsets.Add(edictParser.start);
				}
				else if (cmp < 0) // need to add somewhere after current.word
				{
					while (true)
					{
						cmp = Compare(current.word, word);
						if (cmp == 0)
						{
							current.offsets.Add(edictParser.start);
							break;
						}
						else if (cmp > 0)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Previous = current.Previous;
							temp.Next = current;
							temp.Next.Previous = temp;
							temp.Previous.Next = temp;
							current = temp;
							break;
						}
						if (current.Next == null)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Previous = current;
							current.Next = temp;
							current = temp;
							break;
						}
						current = current.Next;
					}
				}
				else if (cmp > 0)
				{
					while (true)
					{
						cmp = Compare(current.word, word);
						if (cmp == 0)
						{
							current.offsets.Add(edictParser.start);
							break;
						}
						else if (cmp < 0)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Previous = current;
							temp.Next = current.Next;
							temp.Next.Previous = temp;
							temp.Previous.Next = temp;
							current = temp;
							break;
						}
						if (current.Previous == null)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Next = current;
							current.Previous = temp;
							current = temp;
							first = temp;
							break;
						}
						current = current.Previous;
					}
				}

				//if (current.Previous != null && Compare(current.Previous.word, current.word) > 0)
				//{
				//	iter += 0;
				//}
				//if (current.Next != null && Compare(current.word, current.Next.word) > 0)
				//{
				//	iter += 0;
				//}

				if (edictParser.pronoun == null) continue;

				//if (iter == 0x13)
				//{
				//	iter += 0;
				//}

				word = edictParser.pronoun;

				if (WhichIsCloser(current.word, current2.word, word) != 0)
				{
					var temp = current;
					current = current2;
					current2 = temp;
				}

				cmp = Compare(current.word, word);
				if (cmp == 0)
				{
					current.offsets.Add(edictParser.start);
				}
				else if (cmp < 0) // need to add somewhere after current.word
				{
					while (true)
					{
						cmp = Compare(current.word, word);
						if (cmp == 0)
						{
							current.offsets.Add(edictParser.start);
							break;
						}
						else if (cmp > 0)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Previous = current.Previous;
							temp.Next = current;
							temp.Next.Previous = temp;
							temp.Previous.Next = temp;
							current = temp;
							break;
						}
						if (current.Next == null)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Previous = current;
							current.Next = temp;
							current = temp;
							break;
						}
						current = current.Next;
					}
				}
				else if (cmp > 0)
				{
					while (true)
					{
						cmp = Compare(current.word, word);
						if (cmp == 0)
						{
							current.offsets.Add(edictParser.start);
							break;
						}
						else if (cmp < 0)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Next = current.Next;
							temp.Previous = current;
							temp.Next.Previous = temp;
							temp.Previous.Next = temp;
							current = temp;
							break;
						}
						if (current.Previous == null)
						{
							var temp = new IndexEntry(word);
							temp.offsets.Add(edictParser.start);
							temp.Next = current;
							current.Previous = temp;
							current = temp;
							first = temp;
							break;
						}
						current = current.Previous;
					}
				}

				//if (current.Previous != null && Compare(current.Previous.word, current.word) > 0)
				//{
				//	iter += 0;
				//}
				//if (current.Next != null && Compare(current.word, current.Next.word) > 0)
				//{
				//	iter += 0;
				//}

				//if (iter == 0x100) break;

				iter++; //max is 0x00041e0b
			}

			paranoid = false;
			if (paranoid)
			{
				if (check(first) != null) throw new Exception("out of order");
			}

			var memory = new MemoryStream(0x10000);
			memory.WriteByte(0);

			var bytesToWrite = new List<byte>(64);

			current = first;
			while (true)
			{
				memory.Write(current.word, 0, current.word.Length);
				foreach (var offset in current.offsets)
				{
					int num = offset;
					int rem;

					memory.WriteByte(1);

					bytesToWrite.Clear();

					while (true)
					{
						num = Math.DivRem(num, 0x100, out rem);

						//if (rem >= 0 && rem <= 2)
						//if (rem >= 0 && rem <= 3)
						if ((rem >> 2) == 0)
						{
							bytesToWrite.Insert(0, 2);
							rem = (rem << 2) + 2;
							bytesToWrite.Insert(1, (byte)rem);
						}
						else
						{
							bytesToWrite.Insert(0, (byte)rem);
						}

						if (num == 0) break;
					} //end single offset

					foreach (byte b in bytesToWrite)
					{
						memory.WriteByte(b);
					}

				} //end all offsets

				memory.WriteByte(0);

				if (current.Next == null) break;
				current = current.Next;

			} //end all IndexEntry

			edictind = memory.ToArray();

			File.WriteAllBytes(Config.RikaiFilename + ".ind", memory.ToArray());

			paranoid = false;
			if (paranoid)
			{
				current = first;
				if (edictind[0] != 0) throw new Exception();
				int ind = 1;
				while (true)
				{
					var word = new byte[current.word.Length];
					Array.Copy(edictind, ind, word, 0, current.word.Length);
					if (Compare(word, current.word) != 0) throw new Exception();
					ind += current.word.Length;

					int offset_index = 0;
					//current.offsets

					if (edictind[ind] != 1) throw new Exception();
					ind++;

					if (edictind[ind] == 0 || edictind[ind] == 1) throw new Exception();

					int num;
					int rem;
					while (true)
					{
						num = 0;
						while (true)
						{
							rem = edictind[ind++];
							if (rem == 0 || rem == 1) break;
							if ((rem >> 2) == 0) rem = edictind[ind++] >> 2;
							num = num * 0x100 + rem;
						} //end one offset
						if (current.offsets[offset_index] != num) throw new Exception();
						offset_index++;
						if (rem == 0) break;
					} //end all offsets

					if (current.Next == null) break;
					current = current.Next;
				} //end IndexEntry
			} //end paranoid

			//MessageBox.Show($"{Config.RikaiFilename}.ind generated!");
		}
	}
}
