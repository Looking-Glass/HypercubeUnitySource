using UnityEngine;
using System.Collections;


//divs (articulation) explanation
// div cannot == 0.
// if div == 1, then each slice will be defined by a vert in each corner,  at 4 verts per slice
// if div == 2, then each slice will have 3 verts of articulation per axis, for 9 verts total per slice
// if div == 3, then each slice will have 5 verts of articulation per axis
// if div == 4, then each slice will have 9 verts of articulation per axis
// if div == 5, then each slice will have 17 verts of articulation per axis, for 17 * 17 verts total per slice
// if div == 6, then each slice will have 33 verts of articulation per axis
// ...


public class dotCalibrator : MonoBehaviour {

    public Mesh markerMesh;

    public int slices = 10;

    //the subdivisions that will be stored for the calibration.  
    //a value of 5 means 17 * 17 * slices * 2 (x and y) values stored for the calibration.
    //a value of 4 means 9 * 9 * slices * 2 values stored for the calibration.
    const int maxDivs = 4;  // 6 divisions should be absolute max because that means 33 points of articulation in each axis...  we cant ever allow more than 46 because 46 * 46 * 30 slices = 63480 verts.  and this is already extreme 
    const int maxSlices = 30;

    int currentDiv;

    Vector2[,,] positions = new Vector2[(maxDivs * 2) + 1, (maxDivs * 2) + 1, maxSlices];

    public void reset(int d)
    {
    }

    static int getVertsFromDivLevel(int d)
    {
        if (d == 0)
            return 0;
        if (d == 1)
            return 2;

        return (getVertsFromDivLevel(d - 1) * 2) - 1;
    }


    //public void skew()
    //{
    //    //skews
    //    topM.x += skews[s].x;
    //    lowM.x -= skews[s].x;
    //    midL.y += skews[s].y;
    //    midR.y -= skews[s].y;

    //    //interpolate the alternate axis on the skew so that edges will always be straight ( fix elbows caused when we skew)
    //    topM.y = Mathf.Lerp(topL.y, topR.y, Mathf.InverseLerp(topL.x, topR.x, topM.x));
    //    lowM.y = Mathf.Lerp(lowL.y, lowR.y, Mathf.InverseLerp(lowL.x, lowR.x, lowM.x));
    //    midL.x = Mathf.Lerp(topL.x, lowL.x, Mathf.InverseLerp(topL.y, lowL.y, midL.y));
    //    midR.x = Mathf.Lerp(topR.x, lowR.x, Mathf.InverseLerp(topR.y, lowR.y, midR.y));
    //}

    //public void bow()
    //{
    //    //add bow distortion compensation
    //    //bow is stored as top,bottom,left,right  = x y z w
    //    float bowX = 0f;
    //    float bowY = 0f;
    //    float xBowAmount = 0f;
    //    float yBowAmount = 0f;
    //    float averageBowX = (bow.z + bow.w) / 2f;
    //    float averageBowY = (bow.x + bow.y) / 2f;
    //    if (o == shardOrientation.UL)//phase: 1 1
    //    {
    //        xBowAmount = Mathf.Lerp(bow.z, averageBowX, columnLerpValue); //left
    //        yBowAmount = Mathf.Lerp(bow.x, averageBowY, rowLerpValue);  //top
    //        bowX = (1f - Mathf.Cos(1f - rowLerpValue)) * xBowAmount;
    //        bowY = (1f - Mathf.Cos(1f - columnLerpValue)) * yBowAmount;
    //    }
    //    else if (o == shardOrientation.UR)//phase: 1 0
    //    {
    //        xBowAmount = Mathf.Lerp(bow.w, averageBowX, 1f - columnLerpValue); //right
    //        yBowAmount = Mathf.Lerp(bow.x, averageBowY, rowLerpValue);  //top
    //        bowX = (1f - Mathf.Cos(1f - rowLerpValue)) * xBowAmount;
    //        bowY = (1f - Mathf.Cos(0f - columnLerpValue)) * yBowAmount;
    //    }
    //    else if (o == shardOrientation.LL)//phase: 0 1
    //    {
    //        xBowAmount = Mathf.Lerp(bow.z, averageBowX, columnLerpValue); // *rowLerpValue; //left
    //        yBowAmount = Mathf.Lerp(bow.y, averageBowY, 1f - rowLerpValue);  //bottom
    //        bowX = (1f - Mathf.Cos(0f - rowLerpValue)) * xBowAmount;
    //        bowY = (1f - Mathf.Cos(1f - columnLerpValue)) * yBowAmount;
    //    }
    //    else if (o == shardOrientation.LR)//phase: 0 0
    //    {
    //        xBowAmount = Mathf.Lerp(bow.w, averageBowX, 1f - columnLerpValue);//right
    //        yBowAmount = Mathf.Lerp(bow.y, averageBowY, 1f - rowLerpValue);  //bottom
    //        bowX = (1f - Mathf.Cos(0f - rowLerpValue)) * xBowAmount;
    //        bowY = (1f - Mathf.Cos(0f - columnLerpValue)) * yBowAmount;
    //    }

    //    bowX -= xBowAmount * .5f; //the lines above pivot the bowing on the centerpoint of the slice. The two following lines change the pivot to the corner points of articulation so that the center is what moves.
    //    bowY -= yBowAmount * .5f;
    //    lerpedVector.x += bowX;
    //    lerpedVector.y += bowY;
    //    //end bow distortion compensation
    //}



    public string getCalibrationData()
    {
        System.UInt32 divisions = (maxDivs * 2) + 1; //how articulated is the calibration we will be writing

        System.IO.BinaryWriter br = new System.IO.BinaryWriter(new System.IO.MemoryStream());

        for (System.UInt32 s = 0; s < (System.UInt32)slices; s++)
        {
            for (System.UInt32 y = 0; y < divisions; y++)
            {
                for (System.UInt32 x = 0; x < divisions; x++)
                {
                    br.Write((System.Single)positions[x,y,s].x);
                    br.Write((System.Single)positions[x,y,s].y);
                }             
            }
        }
        return br.ToString();
    }

    public static void setCalibrationToMesh(Mesh m, int _xArticulation, int _yArticulation, int _slices, string data)
    {


    }


    public static float[] getCalibrationDataFromBinaryString(string s, int _xArticulation, int _yArticulation, int _slices)
    {
        if (s == "" || _xArticulation < 1 || _yArticulation < 1 || _slices < 1)
            return null;

        System.IO.BinaryReader br = new System.IO.BinaryReader(new System.IO.MemoryStream());

        int length = _xArticulation * _yArticulation * _slices;
        float[] output = new float[length];
        for (int i = 0; i < length; i ++)
        {
            output[i] = System.BitConverter.ToUInt32(br.ReadBytes(4), i * 4); //read 32 bits at a time
        }

        return output;
    }
    
	void Update ()
    {
	
	}
}
