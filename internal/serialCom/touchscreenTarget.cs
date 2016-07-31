using UnityEngine;
using System.Collections;


//this is intended to be used as a virtual base class
//inherit from it to automatically receive touch events from the hypercube
//alternatively, you can 

public class touchscreenTarget : MonoBehaviour
{

	public float leftBorder = 0f;
	public float rightBorder = 1f;
	public float topBorder = 1f;
	public float bottomBorder = 0f;

    void OnEnable()
    {

    }


	public virtual void onTouchDown(Touch touch) 
	{
	}

	public virtual void onTouchUp(Touch touch)
	{
	}

	public virtual void onTouchMoved(Touch touch)
	{
	}

	public virtual void onTouchRelativeMoved(int uid, Vector2 difference) //the difference between the last frame position and the current position
	{
	}


	public Vector2 mapToRange(float x, float y)
	{
		Vector2 position = new Vector2(x, y);
		position.x = map(position.x, 0, 1.0f, leftBorder, rightBorder);
		position.y = map(position.y, 0.0f, 1.0f, bottomBorder, topBorder);
		return position;
	}

	public static float map(float s, float a1, float a2, float b1, float b2)
	{
		return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
	}
}