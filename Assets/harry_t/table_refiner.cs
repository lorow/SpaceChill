#define HT8B_DRAW_REGIONS

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class table_refiner : MonoBehaviour
{
public bool write_table_info = false;

const float k_FIXED_TIME_STEP = 0.0125f;					// time step in seconds per iteration
const float k_FIXED_SUBSTEP	= 0.00125f;
const float k_TIME_ALPHA		= 50.0f;						// (unused) physics interpolation
public float k_TABLE_WIDTH		= 1.064f;					// horizontal span of table
public float k_TABLE_HEIGHT	= 0.605f;					// vertical span of table
const float k_BALL_DIAMETRE	= 0.06f;						// width of ball
const float k_BALL_PL_X			= 0.03f;						// break placement X
const float k_BALL_PL_Y			= 0.05196152422f;			// Break placement Y
const float k_BALL_1OR			= 33.3333333333f;			// 1 over ball radius
const float k_BALL_RSQR			= 0.0009f;					// ball radius squared
const float k_BALL_DSQR			= 0.0036f;					// ball diameter squared
const float k_BALL_DSQRPE		= 0.003598f;				// ball diameter squared plus epsilon
public float k_POCKET_RADIUS	= 0.1f;						// Full diameter of pockets (exc ball radi)
const float k_CUSHION_RSTT		= 0.79f;						// Coefficient of restituion against cushion
const float k_BALL_RADIUS     = 0.03f;
const float k_CUSHION_RADIUS  = 0.043f;

float k_MINOR_REGION_CONST;

float r_k_CUSHION_RADIUS;

const float k_1OR2				= 0.70710678118f;			// 1 over root 2 (normalize +-1,+-1 vector)
const float k_1OR5				= 0.4472135955f;			// 1 over root 5 (normalize +-1,+-2 vector)
const float k_RANDOMIZE_F		= 0.0001f;

const float k_POCKET_DEPTH		= 0.04f;						// How far back (roughly) do pockets absorb balls after this point
const float k_MIN_VELOCITY		= 0.00005625f;				// SQUARED

const float k_FRICTION_EFF		= 0.99f;						// How much to multiply velocity by each update

const float k_F_SLIDE			= 0.2f;						// Friction coefficient of sliding
const float k_F_ROLL				= 0.01f;						// Friction coefficient of rolling

const float k_SPOT_POSITION_X = 0.5334f;					// First X position of the racked balls
const float k_SPOT_CAROM_X		= 0.8001f;					// Spot position for carom mode

const float k_RACK_HEIGHT		= -0.0702f;					// Rack position on Y axis
const float k_GRAVITY			= 9.80665f;					// Earths gravitational acceleration
const float k_BALL_MASS			= 0.16f;						// Weight of ball in kg

public GameObject tester;
public Vector3 _vel;

void _phy_ball_table_std()
{
   float zy, zx, zk, zw, d, k, i, j, l, r;
   Vector3 A, N;

   A = tester.transform.position;

   // REGIONS
   /*  
	   *  QUADS:							SUBSECTION:				SUBSECTION:
	   *    zx, zy:							zz:						zw:
	   *																
	   *  o----o----o  +:  1			\_________/				\_________/
	   *  | -+ | ++ |  -: -1		     |	    /		              /  /
	   *  |----+----|					  -  |  +   |		      -     /   |
	   *  | -- | +- |						  |	   |		          /  +  |
	   *  o----o----o						  |      |             /       |
	   * 
	   */

   // Setup major regions
   zx = Mathf.Sign( A.x );
   zy = Mathf.Sign( A.z );

   // within pocket regions
   if( (A.z*zy > (k_TABLE_HEIGHT-k_POCKET_RADIUS)) && (A.x*zx > (k_TABLE_WIDTH-k_POCKET_RADIUS) || A.x*zx < k_POCKET_RADIUS) )
   {
	   // Subregions
	   zw = A.z * zy > A.x * zx - k_TABLE_WIDTH + k_TABLE_HEIGHT ? 1.0f : -1.0f;

	   // Normalization / line coefficients change depending on sub-region
	   if( A.x * zx > k_TABLE_WIDTH * 0.5f )
	   {
		   zk = 1.0f;
		   r = k_1OR2;
	   }
	   else
	   {
		   zk = -2.0f;
		   r = k_1OR5;
	   }

	   // Collider line EQ
	   d = zx * zy * zk; // Coefficient
	   k = (-(k_TABLE_WIDTH * Mathf.Max(zk, 0.0f)) + k_POCKET_RADIUS * zw * Mathf.Abs( zk ) + k_TABLE_HEIGHT) * zy; // Konstant

	   // Check if colliding
	   l = zw * zy;
	   if( A.z * l > (A.x * d + k) * l )
	   {
		   // Get line normal
		   N.x = zx * zk;
		   N.z = -zy;
		   N.y = 0.0f;
		   N *= zw * r;

		   // New position
		   i = (A.x * d + A.z - k) / (2.0f * d);
		   j = i * d + k;

		   tester.transform.position = new Vector3( i, 0.0f, j );
		   //ball_CO[ id ].x = i;
		   //ball_CO[ id ].z = j;
	   }
   }
   else // edges
   {
	   if( A.x * zx > k_TABLE_WIDTH )
	   {
		   tester.transform.position = new Vector3( k_TABLE_WIDTH * zx, 0.0f, A.z );
	   }

	   if( A.z * zy > k_TABLE_HEIGHT )
	   {
		   tester.transform.position = new Vector3( A.x, 0.0f, k_TABLE_HEIGHT * zy );
	   }
   }
}

Vector3 _V = new Vector3();
Vector3 V = new Vector3();

Vector3 k_vA = new Vector3();
Vector3 k_vB = new Vector3();
Vector3 k_vC = new Vector3();
Vector3 k_vD = new Vector3();

Vector3 k_vX = new Vector3();
Vector3 k_vY = new Vector3();
Vector3 k_vZ = new Vector3();
Vector3 k_vW = new Vector3();

Vector3 k_pK = new Vector3();
Vector3 k_pL = new Vector3();
Vector3 k_pM = new Vector3();
Vector3 k_pN = new Vector3();
Vector3 k_pO = new Vector3();
Vector3 k_pP = new Vector3();
Vector3 k_pQ = new Vector3();
Vector3 k_pR = new Vector3();
Vector3 k_pT = new Vector3();
Vector3 k_pS = new Vector3();
Vector3 k_pU = new Vector3();
Vector3 k_pV = new Vector3();

Vector3 k_vA_vD = new Vector3();
Vector3 k_vA_vD_normal = new Vector3();

Vector3 k_vB_vY = new Vector3();
Vector3 k_vB_vY_normal = new Vector3();

Vector3 k_vC_vZ_normal = new Vector3();

Vector3 k_vA_vB_normal = new Vector3( 0.0f, 0.0f, -1.0f );
Vector3 k_vC_vW_normal = new Vector3( -1.0f, 0.0f, 0.0f );

public Vector3 k_vE = new Vector3();
public Vector3 k_vF = new Vector3();

public float k_INNER_RADIUS = 0.1f;

// Stub
void _phy_bounce_cushion( int id, Vector3 N ) {}

void _phy_table_init()
{
   // Handy values
   k_MINOR_REGION_CONST = k_TABLE_WIDTH - k_TABLE_HEIGHT;

   // Major source vertices
   k_vA.x = k_POCKET_RADIUS * 0.92f;
   k_vA.z = k_TABLE_HEIGHT;

   k_vB.x = k_TABLE_WIDTH-k_POCKET_RADIUS;
   k_vB.z = k_TABLE_HEIGHT;

   k_vC.x = k_TABLE_WIDTH;
   k_vC.z = k_TABLE_HEIGHT-k_POCKET_RADIUS;

   k_vD.x = k_vA.x - 0.016f;
   k_vD.z = k_vA.z + 0.06f;

   // Aux points
   k_vX = k_vD + Vector3.forward;
   k_vW = k_vC - Vector3.forward;

   k_vY = k_vB;
   k_vY.x += 1.0f;
   k_vY.z += 1.0f;

   k_vZ = k_vC;
   k_vZ.x += 1.0f;
   k_vZ.z += 1.0f;

   // Normals
   k_vA_vD = k_vD - k_vA;
   k_vA_vD = k_vA_vD.normalized;
   k_vA_vD_normal.x = -k_vA_vD.z;
   k_vA_vD_normal.z =  k_vA_vD.x;

   k_vB_vY = k_vB - k_vY;
   k_vB_vY = k_vB_vY.normalized;
   k_vB_vY_normal.x = -k_vB_vY.z;
   k_vB_vY_normal.z =  k_vB_vY.x;

   k_vC_vZ_normal = -k_vB_vY_normal;

   // Minkowski difference
   k_pN = k_vA;
   k_pN.z -= r_k_CUSHION_RADIUS;

   k_pM = k_vA + k_vA_vD_normal * r_k_CUSHION_RADIUS;
   k_pL = k_vD + k_vA_vD_normal * r_k_CUSHION_RADIUS;

   k_pK = k_vD;
   k_pK.x -= r_k_CUSHION_RADIUS;

   k_pO = k_vB;
   k_pO.z -= r_k_CUSHION_RADIUS;
   k_pP = k_vB + k_vB_vY_normal * r_k_CUSHION_RADIUS;
   k_pQ = k_vC + k_vC_vZ_normal * r_k_CUSHION_RADIUS;
   
   k_pR = k_vC;
   k_pR.x -= r_k_CUSHION_RADIUS;

   k_pT = k_vX;
   k_pT.x -= r_k_CUSHION_RADIUS;

   k_pS = k_vW;
   k_pS.x -= r_k_CUSHION_RADIUS;

   k_pU = k_vY + k_vB_vY_normal * r_k_CUSHION_RADIUS;
   k_pV = k_vZ + k_vC_vZ_normal * r_k_CUSHION_RADIUS;
  
   k_pS = k_vW;
   k_pS.x -= r_k_CUSHION_RADIUS;
}

string _obj_vec_str( Vector3 v )
{
   return $"v {v.x} {v.y} {v.z}\n";
}

void _phy_draw_circle( Vector3 at, float r )
{
   Vector3 last = at + Vector3.forward * r;
   Vector3 cur = Vector3.zero;

   for( int i = 1; i < 32; i ++ )
   {
      float angle = ((float)i/31.0f)*6.283185307179586f;
      cur.x = at.x + Mathf.Sin( angle ) * r;
      cur.z = at.z + Mathf.Cos( angle ) * r;

      Debug.DrawLine( last, cur, Color.red );
      last = cur;
   }
}

void _phy_table_obj()
{
   r_k_CUSHION_RADIUS = k_CUSHION_RADIUS-k_BALL_RADIUS;
   _phy_table_init();
   Debug.Log( _obj_vec_str(k_pT) + _obj_vec_str(k_pK) + _obj_vec_str(k_pL) + _obj_vec_str(k_pM) + _obj_vec_str(k_pN) + _obj_vec_str(k_pO) + _obj_vec_str(k_pP) + _obj_vec_str(k_pU) + _obj_vec_str(k_pV) + _obj_vec_str(k_pQ) + _obj_vec_str(k_pR) + _obj_vec_str(k_pS) );
}

Vector3 _sign_pos = new Vector3(0.0f,1.0f,0.0f);

void _phy_ball_table_new()
{
   Vector3 A, N, a_to_v;
   float dot;

   A = tester.transform.position;

   int id = 0;
   
   _sign_pos.x = Mathf.Sign( A.x );
   _sign_pos.z = Mathf.Sign( A.z );

   A = Vector3.Scale( A, _sign_pos );

#if HT8B_DRAW_REGIONS
   Debug.DrawLine( k_vA, k_vB, Color.white );
   Debug.DrawLine( k_vD, k_vA, Color.white );
   Debug.DrawLine( k_vB, k_vY, Color.white );
   Debug.DrawLine( k_vD, k_vX, Color.white );
   Debug.DrawLine( k_vC, k_vW, Color.white );
   Debug.DrawLine( k_vC, k_vZ, Color.white );

   r_k_CUSHION_RADIUS = k_CUSHION_RADIUS-k_BALL_RADIUS;

   _phy_table_init();

   Debug.DrawLine( k_pT, k_pK, Color.yellow );
   Debug.DrawLine( k_pK, k_pL, Color.yellow );
   Debug.DrawLine( k_pL, k_pM, Color.yellow );
   Debug.DrawLine( k_pM, k_pN, Color.yellow );
   Debug.DrawLine( k_pN, k_pO, Color.yellow );
   Debug.DrawLine( k_pO, k_pP, Color.yellow );
   Debug.DrawLine( k_pP, k_pU, Color.yellow );

   Debug.DrawLine( k_pV, k_pQ, Color.yellow );
   Debug.DrawLine( k_pQ, k_pR, Color.yellow );
   Debug.DrawLine( k_pR, k_pS, Color.yellow );

   _phy_draw_circle( k_vE, k_INNER_RADIUS );
   _phy_draw_circle( k_vF, k_INNER_RADIUS );

   r_k_CUSHION_RADIUS = k_CUSHION_RADIUS;
   _phy_table_init();
#endif

   if( A.x > k_vA.x ) // Major Regions
   {
      if( A.x > A.z + k_MINOR_REGION_CONST ) // Minor B
      {
         if( A.z < k_TABLE_HEIGHT-k_POCKET_RADIUS )
         {
            // Region H
#if HT8B_DRAW_REGIONS
            Debug.DrawLine( new Vector3( 0.0f, 0.0f, 0.0f ), new Vector3( k_TABLE_WIDTH, 0.0f, 0.0f ), Color.red );
            Debug.DrawLine( k_vC, k_vC + k_vC_vW_normal, Color.red );
#endif
            if( A.x > k_TABLE_WIDTH - k_CUSHION_RADIUS )
            {
               // Static resolution
               A.x = k_TABLE_WIDTH - k_CUSHION_RADIUS;

               // Dynamic
               _phy_bounce_cushion( id, Vector3.Scale( k_vC_vW_normal, _sign_pos ) );
            }
         }
         else
         {
            a_to_v = A - k_vC;

            if( Vector3.Dot( a_to_v, k_vB_vY ) > 0.0f )
            {
               // Region I ( VORONI )
#if HT8B_DRAW_REGIONS
               Debug.DrawLine( k_vC, k_pR, Color.green );
               Debug.DrawLine( k_vC, k_pQ, Color.green );
#endif
               if( a_to_v.magnitude < k_CUSHION_RADIUS )
               {
                  // Static resolution
                  N = a_to_v.normalized;
                  A = k_vC + N * k_CUSHION_RADIUS;

                  // Dynamic
                  _phy_bounce_cushion( id, Vector3.Scale( N, _sign_pos ) );
               }
            }
            else
            {
               // Region J
#if HT8B_DRAW_REGIONS
               Debug.DrawLine( k_vC, k_vB, Color.red );
               Debug.DrawLine( k_pQ, k_pV, Color.blue );
#endif
               a_to_v = A - k_pQ;

               if( Vector3.Dot( k_vC_vZ_normal, a_to_v ) < 0.0f )
               {
                  // Static resolution
                  dot = Vector3.Dot( a_to_v, k_vB_vY );
                  A = k_pQ + dot * k_vB_vY;

                  // Dynamic
                  _phy_bounce_cushion( id, Vector3.Scale( k_vC_vZ_normal, _sign_pos ) );
               }
            }
         }
      }
      else // Minor A
      {
         if( A.x < k_vB.x )
         {
            // Region A
#if HT8B_DRAW_REGIONS
            Debug.DrawLine( k_vA, k_vA + k_vA_vB_normal, Color.red );
            Debug.DrawLine( k_vB, k_vB + k_vA_vB_normal, Color.red );
#endif
            if( A.z > k_pN.z )
            { 
               // Static resolution
               A.z = k_pN.z;

               // Dynamic
               _phy_bounce_cushion( id, Vector3.Scale( k_vA_vB_normal, _sign_pos ) );
            }
         }
         else
         {
            a_to_v = A - k_vB;

            if( Vector3.Dot( a_to_v, k_vB_vY ) > 0.0f )
            {
               // Region F ( VERONI )
#if HT8B_DRAW_REGIONS
               Debug.DrawLine( k_vB, k_pO, Color.green );
               Debug.DrawLine( k_vB, k_pP, Color.green );
#endif
               if( a_to_v.magnitude < k_CUSHION_RADIUS )
               {
                  // Static resolution
                  N = a_to_v.normalized;
                  A = k_vB + N * k_CUSHION_RADIUS;

                  // Dynamic
                  _phy_bounce_cushion( id, Vector3.Scale( N, _sign_pos ) );
               }
            }
            else
            {
               // Region G
#if HT8B_DRAW_REGIONS
               Debug.DrawLine( k_vB, k_vC, Color.red );
               Debug.DrawLine( k_pP, k_pU, Color.blue );
#endif
               a_to_v = A - k_pP;

               if( Vector3.Dot( k_vB_vY_normal, a_to_v ) < 0.0f )
               {
                  // Static resolution
                  dot = Vector3.Dot( a_to_v, k_vB_vY );
                  A = k_pP + dot * k_vB_vY;

                  // Dynamic
                  _phy_bounce_cushion( id, Vector3.Scale( k_vB_vY_normal, _sign_pos ) );
               }
            }
         }
      }
   }
   else
   {
      a_to_v = A - k_vA;

      if( Vector3.Dot( a_to_v, k_vA_vD ) > 0.0f )
      {
         a_to_v = A - k_vD;

         if( Vector3.Dot( a_to_v, k_vA_vD ) > 0.0f )
         {
            if( A.z > k_pK.z )
            {
               // Region E
#if HT8B_DRAW_REGIONS
               Debug.DrawLine( k_vD, k_vD + k_vC_vW_normal, Color.red );
#endif
               if( A.x > k_pK.x )
               {
                  // Static resolution
                  A.x = k_pK.x;

                  // Dynamic
                  _phy_bounce_cushion( id, Vector3.Scale( k_vC_vW_normal, _sign_pos ) );
               }
            }
            else
            {
               // Region D ( VORONI )
#if HT8B_DRAW_REGIONS
               Debug.DrawLine( k_vD, k_vD + k_vC_vW_normal, Color.green );
               Debug.DrawLine( k_vD, k_vD + k_vA_vD_normal, Color.green );
#endif
               if( a_to_v.magnitude < k_CUSHION_RADIUS )
               {
                  // Static resolution
                  N = a_to_v.normalized;
                  A = k_vD + N * k_CUSHION_RADIUS;

                  // Dynamic
                  _phy_bounce_cushion( id, Vector3.Scale( N, _sign_pos ) );
               }
            }
         }
         else
         {
            // Region C
#if HT8B_DRAW_REGIONS
            Debug.DrawLine( k_vA, k_vA + k_vA_vD_normal, Color.red );
            Debug.DrawLine( k_vD, k_vD + k_vA_vD_normal, Color.red );
            Debug.DrawLine( k_pL, k_pM, Color.blue );
#endif
            a_to_v = A - k_pL;

            if( Vector3.Dot( k_vA_vD_normal, a_to_v ) < 0.0f )
            {
               // Static resolution
               dot = Vector3.Dot( a_to_v, k_vA_vD );
               A = k_pL + dot * k_vA_vD;

               // Dynamic
               _phy_bounce_cushion( id, Vector3.Scale( k_vA_vD_normal, _sign_pos ) );
            }
         }
      }
      else
      {
         // Region B ( VORONI )
#if HT8B_DRAW_REGIONS
         Debug.DrawLine( k_vA, k_vA + k_vA_vB_normal, Color.green );
         Debug.DrawLine( k_vA, k_vA + k_vA_vD_normal, Color.green );
#endif
         if( a_to_v.magnitude < k_CUSHION_RADIUS )
         {
            // Static resolution
            N = a_to_v.normalized;
            A = k_vA + N * k_CUSHION_RADIUS;

            // Dynamic
            _phy_bounce_cushion( id, Vector3.Scale( N, _sign_pos ) );
         }
      }
   }

   A = Vector3.Scale( A, _sign_pos );

   tester.transform.position = A;
}

// Update is called once per frame
void Update()
{
   if( write_table_info )
   {
      write_table_info = false;
      _phy_table_obj();
   }
	_phy_ball_table_new();
}

void Start()
{
}
}
