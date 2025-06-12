using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace simplealchemy.src
{

    //
    //https://github.com/DArkHekRoMaNT
    //
    public class Config
    {
        public static Config Current { get; set; } = new Config();
        public class Part<Config>
        {
            public readonly string Comment;
            public readonly Config Default;
            private Config val;
            public Config Val
            {
                get => (val != null ? val : val = Default);
                set => val = (value != null ? value : Default);
            }
            public Part(Config Default, string Comment = null)
            {
                this.Default = Default;
                this.Val = Default;
                this.Comment = Comment;
            }
            public Part(Config Default, string prefix, string[] allowed, string postfix = null)
            {
                this.Default = Default;
                this.Val = Default;
                this.Comment = prefix;

                this.Comment += "[" + allowed[0];
                for (int i = 1; i < allowed.Length; i++)
                {
                    this.Comment += ", " + allowed[i];
                }
                this.Comment += "]" + postfix;
            }
        }
        public Part<Dictionary<string, HashSet<string>>> allowedIngredientsItems = new Part<Dictionary<string, HashSet<string>>>(new Dictionary<string, HashSet<string>>() {
            { "game", new HashSet<string>(){ "beeswax", "bone", "bonemeal", "feather", "fat", "rot", "papyrusroot", "papyrustops", "salt", "redmeat-raw", "bushmeat-raw",
            "powderedsulfur", "gear-rusty", "gear-temporal", "drygrass", "cattailroot", "blastingpowder", "glacierice", "saltpeter", "charcoal"} }

        });


        public Part<Dictionary<string, HashSet<string>>> allowedIngredientsGroupsItems = new Part<Dictionary<string, HashSet<string>>>(new Dictionary<string, HashSet<string>>()
        {
            {"game", new HashSet<string>(){ "mushroom-", "crushed-", "flower-", "treeseed-", "vegetable-", "fruit-", "cheese-", "flour-", "metalbit-", "ingot-" } },
            {"simplealchemy", new HashSet<string>() { "herbingredient-", "mushroomingredient-" } },
            {"em", new HashSet<string>() { "crushed-" } }

        });
        public Part<bool> forgetingPotionWorks = new Part<bool>(false);
        public Part<long> daysBetweenClassChangeWithPotion = new Part<long>(180);

    }
}

