using UnityEngine;

namespace IsotopicSDK.Samples.CheckIsotopicAsset
{
    public class AssetTurntableParent : MonoBehaviour
    {
        public float TurningSpeed = 30f;
        public void Update()
        {
            transform.Rotate(new Vector3(0, Time.deltaTime * TurningSpeed, 0));
        }

        public void SetDisplayObject(GameObject obj)
        {
            foreach (Transform child in transform)
            {
                Destroy(child);
            }

            if (obj.scene.name == null) // means obj is an uninstantiated prefab
            {
                obj = Instantiate(obj, transform);
            }
            obj.transform.position = transform.position;
        }
    }
}
