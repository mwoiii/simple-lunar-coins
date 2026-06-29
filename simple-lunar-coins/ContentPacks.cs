using RoR2;
using RoR2.ContentManagement;
using System.Collections.Generic;

namespace SimpleLunarCoins {
    internal class ContentPacks : IContentPackProvider {
        internal ContentPack contentPack = new ContentPack();
        public string identifier => SimpleLunarCoins.PluginGUID;

        public static List<EffectDef> effectDefs = new List<EffectDef>();

        public void Init() {
            ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;
        }

        private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider) {
            addContentPackProvider(this);
        }

        public System.Collections.IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args) {
            this.contentPack.identifier = this.identifier;

            contentPack.effectDefs.Add(effectDefs.ToArray());

            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args) {
            ContentPack.Copy(this.contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public System.Collections.IEnumerator FinalizeAsync(FinalizeAsyncArgs args) {
            args.ReportProgress(1f);
            yield break;
        }
    }
}