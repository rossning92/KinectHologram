using UnityEngine;
using System.Collections;

[ExecuteInEditMode]

public class ProjectionMatrix : MonoBehaviour
{

    // This script should be attached to a Camera object 
    // in Unity. Once a Plane object is specified as the 
    // "projectionScreen", the script computes a suitable
    // view and projection matrix for the camera.
    // The code is based on Robert Kooima's publication  
    // "Generalized Perspective Projection," 2009, 
    // http://csc.lsu.edu/~kooima/pdfs/gen-perspective.pdf 

    // Use the following line to apply the script in the editor:
    // @script ExecuteInEditMode()
    //Original java script code by https://en.wikibooks.org/wiki/Cg_Programming/Unity/Projection_for_Virtual_Reality
    // Kinect v2 Examples with MS-SDK - https://www.assetstore.unity3d.com/en/#!/content/18708
    //Modified for c# and use with Kinect v2 with MS-SDK by Joshua Fernandes


    public GameObject projectionScreen;
    public bool estimateViewFrustum = true;

    private Camera cameraComponent;
	public bool drawNearCone = true;
	public bool drawFrustum = false;

    void LateUpdate()
    {
        cameraComponent = GetComponent<Camera>();
        if (null != projectionScreen && null != cameraComponent)
        {
            Vector3 pa = projectionScreen.transform.TransformPoint(new Vector3(-5.0f, 0.0f, -5.0f));
            // lower left corner in world coordinates
            Vector3 pb = projectionScreen.transform.TransformPoint(new Vector3(5.0f, 0.0f, -5.0f));
            // lower right corner
            Vector3 pc = projectionScreen.transform.TransformPoint(new Vector3(-5.0f, 0.0f, 5.0f));
			// upper left corner
			Vector3 pd = projectionScreen.transform.TransformPoint(new Vector3(5.0f, 0.0f, 5.0f));


            Vector3 pe = transform.position;
            // eye position
            float n = cameraComponent.nearClipPlane;
            // distance of near clipping plane
            float f = cameraComponent.farClipPlane;
            // distance of far clipping plane

            Vector3 va; // from pe to pa
            Vector3 vb; // from pe to pb
            Vector3 vc; // from pe to pc
			Vector3 vd; // from pe to pd

            Vector3 vr; // right axis of screen
            Vector3 vu; // up axis of screen
            Vector3 vn; // normal vector of screen

            float l; // distance to left screen edge
            float r; // distance to right screen edge
            float b; // distance to bottom screen edge
            float t; // distance to top screen edge
            float d; // distance from eye to screen 

            vr = pb - pa;
            vu = pc - pa;
            vr.Normalize();
            vu.Normalize();
            vn = -Vector3.Cross(vr, vu);
            // we need the minus sign because Unity 
            // uses a left-handed coordinate system
            vn.Normalize();

            va = pa - pe;
            vb = pb - pe;
            vc = pc - pe;
			vd = pd - pe;

            d = -Vector3.Dot(va, vn);
            l = Vector3.Dot(vr, va) * n / d;
            r = Vector3.Dot(vr, vb) * n / d;
            b = Vector3.Dot(vu, va) * n / d;
            t = Vector3.Dot(vu, vc) * n / d;

            Matrix4x4 p = new Matrix4x4(); // projection matrix 
            p[0, 0] = 2.0f * n / (r - l);
            p[0, 1] = 0.0f;
            p[0, 2] = (r + l) / (r - l);
            p[0, 3] = 0.0f;

            p[1, 0] = 0.0f;
            p[1, 1] = 2.0f * n / (t - b);
            p[1, 2] = (t + b) / (t - b);
            p[1, 3] = 0.0f;

            p[2, 0] = 0.0f;
            p[2, 1] = 0.0f;
            p[2, 2] = (f + n) / (n - f);
            p[2, 3] = 2.0f * f * n / (n - f);

            p[3, 0] = 0.0f;
            p[3, 1] = 0.0f;
            p[3, 2] = -1.0f;
            p[3, 3] = 0.0f;

            Matrix4x4 rm = new Matrix4x4(); // rotation matrix;
            rm[0, 0] = vr.x;
            rm[0, 1] = vr.y;
            rm[0, 2] = vr.z;
            rm[0, 3] = 0.0f;

            rm[1, 0] = vu.x;
            rm[1, 1] = vu.y;
            rm[1, 2] = vu.z;
            rm[1, 3] = 0.0f;

            rm[2, 0] = vn.x;
            rm[2, 1] = vn.y;
            rm[2, 2] = vn.z;
            rm[2, 3] = 0.0f;

            rm[3, 0] = 0.0f;
            rm[3, 1] = 0.0f;
            rm[3, 2] = 0.0f;
            rm[3, 3] = 1.0f;

            Matrix4x4 tm = new Matrix4x4(); // translation matrix;
            tm[0, 0] = 1.0f;
            tm[0, 1] = 0.0f;
            tm[0, 2] = 0.0f;
            tm[0, 3] = -pe.x;

            tm[1, 0] = 0.0f;
            tm[1, 1] = 1.0f;
            tm[1, 2] = 0.0f;
            tm[1, 3] = -pe.y;

            tm[2, 0] = 0.0f;
            tm[2, 1] = 0.0f;
            tm[2, 2] = 1.0f;
            tm[2, 3] = -pe.z;

            tm[3, 0] = 0.0f;
            tm[3, 1] = 0.0f;
            tm[3, 2] = 0.0f;
            tm[3, 3] = 1.0f;

            // set matrices
            cameraComponent.projectionMatrix = p;
            cameraComponent.worldToCameraMatrix = rm * tm;
            // The original paper puts everything into the projection 
            // matrix (i.e. sets it to p * rm * tm and the other 
            // matrix to the identity), but this doesn't appear to 
            // work with Unity's shadow maps.

            if (estimateViewFrustum)
            {
                // rotate camera to screen for culling to work
                Quaternion q = new Quaternion();
                q.SetLookRotation((0.5f * (pb + pc) - pe), vu);
                // look at center of screen
                cameraComponent.transform.rotation = q;

                // set fieldOfView to a conservative estimate 
                // to make frustum tall enough
                if (cameraComponent.aspect >= 1.0)
                {
                    cameraComponent.fieldOfView = Mathf.Rad2Deg *
                       Mathf.Atan(((pb - pa).magnitude + (pc - pa).magnitude)
                       / va.magnitude);
                }
                else
                {
                    // take the camera aspect into account to 
                    // make the frustum wide enough 
                    cameraComponent.fieldOfView =
                       Mathf.Rad2Deg / cameraComponent.aspect *
                       Mathf.Atan(((pb - pa).magnitude + (pc - pa).magnitude)
                       / va.magnitude);
                }
            }

			if ( drawNearCone ) { //Draw lines from the camera to the corners f the screen
				Debug.DrawRay( cameraComponent.transform.position, va, Color.blue );
				Debug.DrawRay( cameraComponent.transform.position, vb, Color.blue );
				Debug.DrawRay( cameraComponent.transform.position, vc, Color.blue );
				Debug.DrawRay( cameraComponent.transform.position, vd, Color.blue );
			}

			if ( drawFrustum ) DrawFrustum( cameraComponent ); //Draw actual camera frustum
        }
    }

	Vector3 ThreePlaneIntersection ( Plane p1, Plane p2, Plane p3 ) { //get the intersection point of 3 planes
		return ( ( -p1.distance * Vector3.Cross( p2.normal, p3.normal ) ) +
			( -p2.distance * Vector3.Cross( p3.normal, p1.normal ) ) +
			( -p3.distance * Vector3.Cross( p1.normal, p2.normal ) ) ) /
			( Vector3.Dot( p1.normal, Vector3.Cross( p2.normal, p3.normal ) ) );
	}

	void DrawFrustum ( Camera cam ) {
		Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
		Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
		Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes( cam ); //get planes from matrix
		Plane temp = camPlanes[1]; camPlanes[1] = camPlanes[2]; camPlanes[2] = temp; //swap [1] and [2] so the order is better for the loop

		for ( int i = 0; i < 4; i++ ) {
			nearCorners[i] = ThreePlaneIntersection( camPlanes[4], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //near corners on the created projection matrix
			farCorners[i] = ThreePlaneIntersection( camPlanes[5], camPlanes[i], camPlanes[( i + 1 ) % 4] ); //far corners on the created projection matrix
		}

		for ( int i = 0; i < 4; i++ ) {
			Debug.DrawLine( nearCorners[i], nearCorners[( i + 1 ) % 4], Color.red, Time.deltaTime, false ); //near corners on the created projection matrix
			Debug.DrawLine( farCorners[i], farCorners[( i + 1 ) % 4], Color.red, Time.deltaTime, false ); //far corners on the created projection matrix
			Debug.DrawLine( nearCorners[i], farCorners[i], Color.red, Time.deltaTime, false ); //sides of the created projection matrix
		}
	}
}
