using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MinorShift.Emuera;
using MinorShift.Emuera.GameView;

namespace WebEmuera.Pages
{
    public partial class Index
    {
        [Inject]
        public HttpClient HttpClient { get; private set; }
        [Inject]
        private JSRuntime JSRuntime { get; set; }


        public string InternalEmueraVer { get { return "1.824.0.0"; } }

        //public string EmueraVerText { get { return EmuVerToolStripTextBox.Text; } }
        public string EmueraVerText { get { return "1.824.*"; } }

        bool PressEnterKeyInProcess;
        bool changeTextbyMouse = false;
		private EmueraConsole console = null;




        string[] prevInputs = new string[100];
        int selectedInputs = 100;
        int lastSelected = 100;


        protected override async Task OnParametersSetAsync()
        {
            base.OnParametersSet();
            GlobalStatic.JSRuntime = JSRuntime;
            GlobalStatic.HttpClient = HttpClient;



            MinorShift.Emuera.Program.Main(null);
            await Task.Run(InitConsole);
        }



        private void InitConsole()
        {
            console = new EmueraConsole(this);
            /*
			DrawTextUtils.Reset();
			DrawBitmapUtils.Reset();
			*/
            console.Initialize();
        }

            
        public new void StateHasChanged()
        {
           base.StateHasChanged();
        }
    }
}
