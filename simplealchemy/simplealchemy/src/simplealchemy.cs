using effectshud.src;
using HarmonyLib;
using Newtonsoft.Json;
using simplealchemy.src.gui;
using simplealchemy.src.item;
using simplealchemy.src.recipies;
using simplesimplealchemy.src;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using static simplealchemy.src.recipies.RecipeSystem;

namespace simplealchemy.src
{
    public class simplealchemy: ModSystem
    {
        public static ICoreServerAPI sapi;
        public static Dictionary<string, long> lastPlayerClassChange;
        public static List<PotionCauldronRecipe> potionCauldronRecipes;
        public static Harmony harmonyInstance;
        public const string harmonyID = "blacksmithname.Patches";
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("BlockPotionFlask", typeof(BlockPotionFlask));
            api.RegisterBlockEntityClass("BlockEntityPotionFlask", typeof(BlockEntityPotionFlask));
            api.RegisterItemClass("ItemPotion", typeof(ItemPotion));
            api.RegisterItemClass("ItemAlchemyRecipesBook", typeof(ItemAlchemyRecipesBook));
            api.RegisterBlockClass("BlockPotionCauldron", typeof(BlockPotionCauldron));
            api.RegisterBlockEntityClass("BlockEntityPotionCauldron", typeof(BlockEntityPotionCauldron));
            //harmonyInstance = new Harmony(harmonyID);
            //harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("GetHeldItemInfo"), prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_GetHeldItemInfo")));

        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            sapi = api;
            loadConfig();

            var s = sapi.WorldManager.SaveGame.GetData<Dictionary<string, long>>("simplealchemylastPlayerClassChange");
            if (s != null)
            {
                lastPlayerClassChange = s;
            }
            else
            {
                lastPlayerClassChange = new Dictionary<string, long>();
            }
        }

        public void loadConfig()
        {
            try
            {                
                Config.Current = sapi.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
                if (Config.Current != null)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                sapi.Logger.Error("loadConfig::" + e.Message);
            }

            Config.Current = new Config();
            sapi.StoreModConfig<Config>(Config.Current, this.Mod.Info.ModID + ".json");
            return;
        }

        public override void Dispose()
        {
            base.Dispose(); 
            if (lastPlayerClassChange != null && lastPlayerClassChange.Count != 0)
            {
                sapi.WorldManager.SaveGame.StoreData<Dictionary<string, long>>("simplealchemylastPlayerClassChange", lastPlayerClassChange);
            }
        }
    }
}
