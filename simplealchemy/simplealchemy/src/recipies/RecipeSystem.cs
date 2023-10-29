using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using static simplealchemy.src.recipies.RecipeSystem;

namespace simplealchemy.src.recipies
{
    public class RecipeSystem
    {
        public class PotionCauldronRecipeRegistry<T> : RecipeRegistryBase where T : IByteSerializable, new ()
        {

            public List<PotionCauldronRecipe> Recipes;

            public PotionCauldronRecipeRegistry()
            {
                Recipes = new List<PotionCauldronRecipe>();
            }

            public PotionCauldronRecipeRegistry(List<PotionCauldronRecipe> recipes)
            {
                Recipes = recipes;
            }

            public override void FromBytes(IWorldAccessor resolver, int quantity, byte[] data)
            {
                using MemoryStream input = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(input);
                for (int i = 0; i < quantity; i++)
                {
                    PotionCauldronRecipe item = new PotionCauldronRecipe();
                    item.FromBytes(reader, resolver);
                    Recipes.Add(item);
                }
            }

            public override void ToBytes(IWorldAccessor resolver, out byte[] data, out int quantity)
            {
                quantity = Recipes.Count;
                using MemoryStream memoryStream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(memoryStream);
                foreach (PotionCauldronRecipe recipe in Recipes)
                {
                    recipe.ToBytes(writer);
                }

                data = memoryStream.ToArray();
            }
        }

        public class PotionCauldronRecipeLoader : RecipeLoader
        {
            /*public List<PotionCauldronRecipe> PotionCauldronRecipes
            {
                get
                {
                    return this.potionCauldronRecipes;
                }
                set
                {
                    this.potionCauldronRecipes = value;
                }
            }
            private List<PotionCauldronRecipe> potionCauldronRecipes = new List<PotionCauldronRecipe>();*/
            public override double ExecuteOrder()
            {
                return 100.0;
            }
            public override bool ShouldLoad(EnumAppSide side)
            {
                return true;
                return base.ShouldLoad(side);
            }
            public override void Start(ICoreAPI api)
            {
                base.Start(api);
                 simplealchemy.potionCauldronRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<PotionCauldronRecipe>>("potionrecipes").Recipes;
            }
            public override void AssetsLoaded(ICoreAPI api)
            {
                LoadPotionCauldronRecipes(api);
                var c = 3;
            }
            public override void StartServerSide(ICoreServerAPI api)
            {
                this.api = api;
               // api.Event.SaveGameLoaded += this.LoadFoodRecipes;
            }

            public override void Dispose()
            {
                base.Dispose();
            }

            public void LoadFoodRecipes()
            {
                this.LoadPotionCauldronRecipes(api);
            }

            public void LoadFoodRecipesClient(IClientPlayer byPlayer)
            {
                capi.Event.RegisterCallback((dt =>
                {
                    this.LoadPotionCauldronRecipes(capi);
                }
                ), 30 * 1000);
               
            }

            public void LoadPotionCauldronRecipes(ICoreAPI api)
            {
                if (api.Side != EnumAppSide.Server)
                {
                    return;
                }
                Dictionary<AssetLocation, JToken> many = null;
                if (api.Side == EnumAppSide.Server)
                {
                     many = api.Assets.GetMany<JToken>(api.Logger, "recipes/potioncauldron", null);
                }
                int num = 0;
                foreach (KeyValuePair<AssetLocation, JToken> keyValuePair in many)
                {
                    bool flag = keyValuePair.Value is JObject;
                    if (flag)
                    {
                        PotionCauldronRecipe potionCauldronRecipe = keyValuePair.Value.ToObject<PotionCauldronRecipe>();
                        bool flag2 = !potionCauldronRecipe.Enabled;
                        if (flag2)
                        {
                            continue;
                        }
                        PotionCauldronRecipe potionCauldronRecipe2 = potionCauldronRecipe;
                        IWorldAccessor world = api.World;
                        string str = "mixing recipe ";
                        AssetLocation key = keyValuePair.Key;
                        potionCauldronRecipe2.Resolve(world, str + ((key != null) ? key.ToString() : null));

                        simplealchemy.potionCauldronRecipes.Add(potionCauldronRecipe);
                        num++;
                    }
                    bool flag3 = keyValuePair.Value is JArray;
                    if (flag3)
                    {
                        foreach (JToken jtoken in (keyValuePair.Value as JArray))
                        {
                            PotionCauldronRecipe potionCauldronRecipe3 = jtoken.ToObject<PotionCauldronRecipe>();
                            bool flag4 = !potionCauldronRecipe3.Enabled;
                            if (!flag4)
                            {
                                PotionCauldronRecipe potionCauldronRecipe4 = potionCauldronRecipe3;
                                IWorldAccessor world2 = api.World;
                                string str2 = "mixing recipe ";
                                AssetLocation key2 = keyValuePair.Key;
                                potionCauldronRecipe4.Resolve(world2, str2 + ((key2 != null) ? key2.ToString() : null));
                                simplealchemy.potionCauldronRecipes.Add(potionCauldronRecipe3);
                                num++;
                            }
                        }
                    }
                }
                api.World.Logger.Event("{0} potioncauldron recipes loaded", new object[]
                {
                    num
                });
            }

            public ICoreServerAPI api;
            public ICoreClientAPI capi;
        }
    }
}
