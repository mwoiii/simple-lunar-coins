using UnityEngine;
using RoR2.ContentManagement;
using System.Collections;
using Path = System.IO.Path;


namespace SimpleLunarCoins
{
    public static class Assets
    {
        public static AssetBundle mainAssetBundle = null;
        //the filename of your assetbundle
        internal static string assetBundleName = "mwassetbundle";

        internal static string assemblyDir
        {
            get
            {
                return Path.GetDirectoryName(SimpleLunarCoins.pluginInfo.Location);
            }
        }

        public static void PopulateAssets()
        {
            mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(assemblyDir, assetBundleName));
            ContentPackProvider.serializedContentPack = mainAssetBundle.LoadAsset<SerializableContentPack>(ContentPackProvider.contentPackName);
        }
    }

    public class ContentPackProvider : IContentPackProvider
    {
        public static SerializableContentPack serializedContentPack;
        public static ContentPack contentPack;
        //Should be the same names as your SerializableContentPack in the asset bundle
        public static string contentPackName = "CoinPack";

        public string identifier
        {
            get
            {
                return "SimpleLunarCoins";
            }
        }

        internal static void Initialize()
        {
            contentPack = serializedContentPack.CreateContentPack();
            ContentManager.collectContentPackProviders += AddCustomContent;
        }

        private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new ContentPackProvider());
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }

    internal static class SoundBanks
    {
        private static bool initialized = false;
        public static string SoundBankDirectory
        {
            get
            {
                return Path.Combine(Assets.assemblyDir);
            }
        }

        public static void Init()
        {
            if (initialized) return;
            initialized = true;
            AKRESULT akResult = AkSoundEngine.AddBasePath(SoundBankDirectory);
            if (akResult == AKRESULT.AK_Success)
            {
                Log.Info($"Added bank base path : {SoundBankDirectory}");
            }
            else
            {
                Log.Error(
                    $"Error adding base path : {SoundBankDirectory} " +
                    $"Error code : {akResult}");
            }

            AkSoundEngine.LoadBank("cointoss.bnk", out _);
            if (akResult == AKRESULT.AK_Success)
            {
                Log.Info($"Added bank : {"cointoss.bnk"}");
            }
            else
            {
                Log.Error(
                    $"Error loading bank : {"cointoss.bnk"} " +
                    $"Error code : {akResult}");
            }
        }
    }
}