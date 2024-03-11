using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IsotopicSDK.OAuthScene
{
    public class IsotopicOAuthUIManager : MonoBehaviour
    {
        public TextMeshProUGUI CodeTextTMP;
        public TextMeshProUGUI TMPTextLoading;
        public Image ImageLoading;

        public GameObject OAuthLoadingPanelParent;
        public GameObject OAuthCodePanelParent;


        public void SetOAuthCodeUI(string code)
        {
            OAuthLoadingPanelParent.SetActive(false);
            OAuthCodePanelParent.SetActive(true);
            CodeTextTMP.text = code;
        }

        public void SetOAuthUILoading(string usernameWelcome, Texture2D profilePic)
        {
            OAuthLoadingPanelParent.SetActive(true);
            OAuthCodePanelParent.SetActive(false);
            if (usernameWelcome == null || profilePic == null) return;
            TMPTextLoading.text = "Welcome " + usernameWelcome + "!";
            ImageLoading.sprite = Sprite.Create(profilePic, ImageLoading.sprite.rect, ImageLoading.sprite.pivot);
            ImageLoading.GetComponent<AspectRatioFitter>().aspectRatio = ImageLoading.sprite.texture.width / ImageLoading.sprite.texture.height;
        }

        public void OpenIsotopicLoginLink()
        {
            Application.OpenURL("https://isotopic.io/login");
        }
    }

}
