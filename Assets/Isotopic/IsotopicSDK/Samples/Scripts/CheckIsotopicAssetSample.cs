using UnityEngine;

namespace IsotopicSDK.Samples.CheckIsotopicAsset
{
    public class CheckIsotopicAssetSample : MonoBehaviour
    {

        public AssetTurntableParent AssetTurntableParent;
        private void Awake()
        {
            if (Isotopic.Web3Instance == null) throw new System.Exception("Web3 Instance Uninitialized. If you are getting this error you are probably trying to perform some action that requires the Isotopic web3 Instance, but have not intialized it yet.");
            if (Isotopic.User.UserProfile == null) throw new System.Exception("Isotopic User not logged in. If you are getting this error it probably means you need to first login the user via Isotopic OAuth.");


            Isotopic.Network.IsotopicAssets.GetIsotopicAssetBalance(Isotopic.SDKConfig.IsotopicAssets[0], res =>
            {
                if (res > 0)
                {
                    Debug.Log("User owns " + res.ToString() + " asset(s).");
                    AssetTurntableParent.SetDisplayObject(Isotopic.SDKConfig.IsotopicAssets[0].AssetPrefab);
                }
                else
                {
                    Debug.Log("Asset not owned on any linked wallet");
                }
            });
        }
    }

}
