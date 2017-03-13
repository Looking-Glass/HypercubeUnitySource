using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace hypercube
{
    [ExecuteInEditMode]
    public class slicePostProcess : MonoBehaviour
    {
        public enum blending
        {
            NONE = 0,
            OVER,
            UNDER,
            ADD,
            MULTIPLY,
            REPLACE
        }

        public blending blend;
        public Texture tex;

        Material over;
        Material under;
        Material adding;
        Material multiplying;

        int texID = -1;

        private void Awake()
        {
            texID = Shader.PropertyToID("_blend");
            over = new Material(Shader.Find("Hidden/s_over"));
            under = new Material(Shader.Find("Hidden/s_under"));
            adding = new Material(Shader.Find("Hidden/s_adding"));
            multiplying = new Material(Shader.Find("Hidden/s_multiplying"));
        }

        //postprocess the image
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (blend == blending.NONE || tex == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (blend == blending.UNDER)
            {
                under.SetTexture(texID, tex);
                Graphics.Blit(source, destination, under);
            }
            else if (blend == blending.OVER)
            {
                over.SetTexture(texID, tex);
                Graphics.Blit(source, destination, over);
            }
            else if (blend == blending.ADD)
            {
                adding.SetTexture(texID, tex);
                Graphics.Blit(source, destination, adding);
            }
            else if (blend == blending.MULTIPLY)
            {
                multiplying.SetTexture(texID, tex);
                Graphics.Blit(source, destination, multiplying);
            }
            else if (blend == blending.REPLACE)
            {
                Graphics.Blit(tex, destination);
            }

            //add other blending type here if needed...
        }
    }
}
