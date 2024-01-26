using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using CitizenFX.Core.UI;
using static CitizenFX.Core.Native.API;

namespace DuiHandler
{
    public class DuiContainer
    {
        public int TargetHandle { get; set; }
        public long TxdHandle { get; set; }
        public List<Prop> Props = new List<Prop>();
        public String RenderTargetName { get; set; }
        public long duiObj { get; set; }
        public string UniqId { get; set; }

        public void Draw()
        {
            SetTextRenderId(TargetHandle);
            SetScriptGfxDrawOrder(4);
            SetScriptGfxDrawBehindPausemenu(true);
            DrawRect(0.5f, 0.5f, 1.0f, 1.0f, 0, 0, 0, 255);
            DrawSprite($"{this.RenderTargetName}-{UniqId}", $"{this.RenderTargetName}-{UniqId}-test", 0.5f, 0.5f, 1.0f, 1.0f, 0.0f, 255, 255, 255, 255);
            //DrawSprite('fib_pc', 'arrow', mX / screenWidth, mY / screenHeight, 0.02, 0.02, 0.0, 255, 255, 255, 255)
            SetTextRenderId(GetDefaultScriptRendertargetRenderId());
            SetScriptGfxDrawBehindPausemenu(false);
        }

    }

    public class DuiHandler : BaseScript
    {
        public Dictionary<string, List<string>> RenderTargets { get; set; }
        public Dictionary<string, List<DuiContainer>> DuiContainers { get; set; } = new Dictionary<string, List<DuiContainer>>();
        public List<String> UsedRenderTargets = new List<string>();
        public Dictionary<string, List<DuiContainer>> DeletedContainers = new Dictionary<string, List<DuiContainer>>();
        public DuiHandler()
        {
            Exports.Add("createDui", new Func<string, string, Task<DuiContainer>>(AddDui));
            Exports.Add("CreateRandomUniqueDuiContainer", new Func<string, Task<DuiContainer>>(CreateRandomUniqueDuiContainer));
            Exports.Add("destroyAllDui", new Func<Task>(DestroyAllDui));
            var renderTargetsJson = LoadResourceFile("addon", "renderTargets.json");
            RenderTargets = JsonConvert.DeserializeObject<Dictionary<String, List<String>>>(renderTargetsJson);

            this.Tick += DuiHandler_Tick;
        }

        public async Task<DuiContainer> CreateRandomUniqueDuiContainer(string url)
        {
            var keys = RenderTargets.Keys;
            var random = new Random();
            string renderName = null;

            while (UsedRenderTargets.Contains(renderName) || renderName == null)
            {
                renderName = keys.ElementAt(random.Next(0, keys.Count));
            }

            return await this.AddDui(renderName, url);
        }

        private async Task DestroyAllDui()
        {
            List<DuiContainer> containers = new List<DuiContainer>();
            foreach (var renderTargetName in DuiContainers)
            {
                foreach (var duiContainer in renderTargetName.Value)
                {
                    containers.Add(duiContainer);
                }
            }

            foreach (var duiContainer in containers)
            {
                RemoveDuiContainer(duiContainer);
            }
        }

        private void RemoveDuiContainer(DuiContainer duiContainer)
        {
            SetDuiUrl(duiContainer.duiObj, "about:blank");
            //DestroyDui(duiContainer.duiObj);

            foreach (var prop in duiContainer.Props)
            {
                prop.Delete();
            }

            if (!DeletedContainers.ContainsKey(duiContainer.RenderTargetName))
                DeletedContainers[duiContainer.RenderTargetName] = new List<DuiContainer>();

            DeletedContainers[duiContainer.RenderTargetName].Add(duiContainer);
            DuiContainers[duiContainer.RenderTargetName].Remove(duiContainer);
            if (!DuiContainers[duiContainer.RenderTargetName].Any())
                DuiContainers.Remove(duiContainer.RenderTargetName);
        }

        private async Task DuiHandler_Tick()
        {
            foreach (var renderTargetName in DuiContainers)
            {
                foreach (var duiContainer in renderTargetName.Value)
                {
                    duiContainer.Draw();
                }
            }
        }

        public async Task<DuiContainer> AddDui(String renderTarget, string url)
        {
            //RemoveIpl("ex_dt1_11_office_01a");
            //RequestIpl("ex_dt1_11_office_01a");
            DuiContainer duiContainer = null;
            if (DeletedContainers.ContainsKey(renderTarget) && DeletedContainers[renderTarget].Any())
            {
                duiContainer = ReuseDuiContainer(renderTarget, url);
            }

            var propAndModelName = await CreateModelForRender(renderTarget);
            var modelName = propAndModelName.Item2;
            if (duiContainer == null)
                duiContainer = AddDuiInternal(renderTarget, modelName, url);
            duiContainer.Props = new List<Prop>() { propAndModelName.Item1 };
            if (!DuiContainers.ContainsKey(renderTarget))
                DuiContainers.Add(renderTarget, new List<DuiContainer>());

            DuiContainers[renderTarget].Add(duiContainer);
            if (!UsedRenderTargets.Contains(renderTarget))
            {
                //ReleaseNamedRendertarget(duiContainer.RenderTargetName);
                UsedRenderTargets.Add(renderTarget);
            }
            return duiContainer;
        }

        private DuiContainer ReuseDuiContainer(string renderTarget, string url)
        {
            DuiContainer duiContainer = DeletedContainers[renderTarget][0];
            DeletedContainers[renderTarget].Remove(duiContainer);
            if (!DeletedContainers[renderTarget].Any())
            {
                DeletedContainers.Remove(renderTarget);
            }
            SetDuiUrl(duiContainer.duiObj, url);
            return duiContainer;
        }

        private async Task<Tuple<Prop, string>> CreateModelForRender(String renderTarget, String modelName = null)
        {
            if (modelName == null)
            {
                var random = new Random();
                var props = this.RenderTargets[renderTarget];
                modelName = props[random.Next(0, props.Count)];
            }
            Model model = new Model(modelName);
            model.Request();

            var prop = await World.CreateProp(model, Game.PlayerPed.Position + Game.PlayerPed.ForwardVector * 3, false, false);
            prop.IsCollisionEnabled = false;
            prop.Heading = Game.PlayerPed.Heading;
            return new Tuple<Prop, String>(prop, modelName);
        }

        private DuiContainer AddDuiInternal(string renderTarget, string modelName, string url)
        {
            var duiContainer = new DuiContainer();
            var res = SetupScreen(url, renderTarget, modelName);
            duiContainer.TxdHandle = res.Item1;
            duiContainer.TargetHandle = res.Item2;
            duiContainer.duiObj = res.Item3;
            duiContainer.UniqId = res.Item4;
            duiContainer.RenderTargetName = renderTarget;
            return duiContainer;
        }

        private Tuple<long, int, long, string> SetupScreen(String url, String renderTargetName, String modelName)
        {
            var model = GetHashKey(modelName);
            var uniqID = Guid.NewGuid();

            var scale = 1.5;
            var screenWidth = Math.Floor(Screen.Width / scale);
            var screenHeight = Math.Floor(Screen.Height / scale);
            var handle = CreateNamedRenderTargetForModel(renderTargetName, (uint)model);
            var txd = CreateRuntimeTxd($"{renderTargetName}-{uniqID}");
            var duiObj = CreateDui(url, (int)screenWidth, (int)screenHeight);

            var dui = GetDuiHandle(duiObj);
            var tx = CreateRuntimeTextureFromDuiHandle(txd, $"{renderTargetName}-{uniqID}-test", dui);

            return new Tuple<long, int, long, string>(tx, handle, duiObj, uniqID.ToString());
        }

        private int CreateNamedRenderTargetForModel(string name, uint model)
        {
            var handle = 0;
            if (!IsNamedRendertargetRegistered(name))
            {
                RegisterNamedRendertarget(name, false);
            }

            if (!IsNamedRendertargetLinked(model))
            {
                LinkNamedRendertarget(model);
            }

            if (IsNamedRendertargetRegistered(name))
                handle = GetNamedRendertargetRenderId(name);
            return handle;
        }
    }
}
