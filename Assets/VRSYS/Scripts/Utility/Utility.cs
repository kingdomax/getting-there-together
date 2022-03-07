using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vrsys
{
    public class Utility
    {
        public static GameObject FindRecursiveInScene(Scene scene, string name)
        {
            var sceneRoots = scene.GetRootGameObjects();

            GameObject result = null;
            foreach (var root in sceneRoots)
            {
                if (root.name.Equals(name)) return root;

                result = FindRecursive(root, name);

                if (result) break;
            }

            return result;
        }

        public static GameObject FindRecursiveInScene(string name, Scene? scn = null)
        {
            Scene scene;

            if (scn == null)
                scene = SceneManager.GetActiveScene();
            else
                scene = (Scene)scn;

            var sceneRoots = scene.GetRootGameObjects();

            GameObject result = null;
            foreach (var root in sceneRoots)
            {
                if (root.name.Equals(name)) return root;

                result = FindRecursive(root, name);

                if (result) break;
            }

            return result;
        }

        public static GameObject FindRecursive(GameObject entryGO, string name)
        {
            GameObject result = null;            
            foreach (Transform child in entryGO.transform)
            {
                if (child.name.Equals(name))
                    return child.gameObject;

                result = FindRecursive(child.gameObject, name);

                if (result != null)
                    break;
            }
            return result;
        }


        public static Color ParseColorFromPrefs()
        {
            Color color;
            switch (PlayerPrefs.GetString("UserColor"))
            {
                case "ColorBlack": color = new Color(.2f, .2f, .2f); break;
                case "ColorRed": color = new Color(1f, 0f, 0f); break;
                case "ColorGreen": color = new Color(0f, 1f, 0f); break;
                case "ColorBlue": color = new Color(0f, 0f, 1f); break;
                case "ColorPink": color = new Color(255f / 255f, 192f / 255f, 203 / 255f); break;
                case "ColorWhite": color = new Color(1f, 1f, 1f); break;
                default: color = new Color(0.6f, 0.6f, 0.6f); break;
            }
            return color;
        }

        public static int LayermaskToLayer(LayerMask layerMask)
        {
            int layerNumber = 0;
            int layer = layerMask.value;
            while (layer > 0)
            {
                layer = layer >> 1;
                layerNumber++;
            }
            return layerNumber - 1;
        }
    }
}