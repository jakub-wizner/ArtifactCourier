using ArtifactCourier.Core;
using UnityEngine;
namespace ArtifactCourier.Player
{
    public sealed class CosmeticCatalog : ScriptableObject
    {
        public Sprite[] skins;
        public Sprite[] originals;
        public Material keyedMaterial;
        static CosmeticCatalog cached;
        public static CosmeticCatalog Instance => cached!=null?cached:cached=Resources.Load<CosmeticCatalog>("CosmeticCatalog");
        public static int Price(int tier)=>tier==0?100:tier==1?250:450;
        public static int UnlockLevel(int tier)=>2+tier*2;
        public static int Equipped(SaveData data,int vehicle)
        {return data.equippedCosmetics!=null&&data.equippedCosmetics.Length==3?Mathf.Clamp(data.equippedCosmetics[vehicle],0,3):0;}
    }
}
