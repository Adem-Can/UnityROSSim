using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;

public class LidarSimulator : MonoBehaviour
{
    public float fovX = 90; //Vertical
    public float fovY = 90; //Horizontal
    public int xLines = 7;
    public int yLines = 7;
    public float rayLength = 10;
    public bool visualize;
    
    public float maxuncertain = 0;
    
    public string pubTopic;
    public float publishDelay;
    public ROSConnection ros;
    
    private float timeElapsed;
    public PointFieldMsg[] fieldsDesc = new PointFieldMsg[4];
    
    public List<Vector3> rays;
    
    // Start is called before the first frame update
    void Start()
    {
        ros.RegisterPublisher<PointCloud2Msg>(pubTopic);
        visualize = true;
        
        //Preconstruct the pointfield field for ROS-Pointcloud messages
        //Just hardcode this in the exact same way the Blickfeld Cube does it
        fieldsDesc[0].name = "x";
        fieldsDesc[0].offset = 0;
        fieldsDesc[0].datatype = 7;
        fieldsDesc[0].count = 1;
        
        fieldsDesc[1].name = "y";
        fieldsDesc[1].offset = 4;
        fieldsDesc[1].datatype = 7;
        fieldsDesc[1].count = 1;
        
        fieldsDesc[2].name = "z";
        fieldsDesc[2].offset = 8;
        fieldsDesc[2].datatype = 7;
        fieldsDesc[2].count = 1;
        
        fieldsDesc[3].name = "intensity";
        fieldsDesc[3].offset = 12;
        fieldsDesc[3].datatype = 6;
        fieldsDesc[3].count = 1;
        
        //Always have an odd number of lines
        if((xLines % 2) == 0)
        {
            xLines++;
        }
        if((yLines % 2) == 0)
        {
            yLines++;
        }
        
        //Calculate the sensor ray vectors
        //These are dirdctional vectors, meant to be added to the current position for a raycast
        
        //First we setup the horizontal lines.
        //We do this by first taking the forward vector of this lidar,
        //copying it yLines-1 times and rotating those copies around 
        //the local y-axis (transform.up) according to fovY.
        //Then, the collection of these vectors is copied xLines-1 times and rotated 
        //around the local x-axis (transform.right) according to fovX. 
        //The resulting collection of vectors
        //contains all sensor rays for this lidar. At the end, there will be a total
        //of xLines*yLines sensor rays.
        List<Vector3> raysY = new List<Vector3>();
        
        //Add the forward vector (middle)
        Vector3 middle = transform.forward * rayLength;
        raysY.Add(middle);
        
        //Copy and rotate for y
        int linesToAddPerSideY = (yLines - 1) / 2;
        //Debug.Log("Lines to add per side: " + linesToAddPerSideY);
        
        for(int i = 1; i <= linesToAddPerSideY; i++)
        {
            float angle = (fovY/2.0f) * (((float)i) / ((float)linesToAddPerSideY));
            Quaternion rotationL = Quaternion.AngleAxis(angle, transform.up);
            Quaternion rotationR = Quaternion.AngleAxis(-angle, transform.up);
            Vector3 addLeft = rotationL * middle;
            Vector3 addRight = rotationR * middle;
            raysY.Add(addLeft);
            raysY.Add(addRight);
        }
        
        foreach(Vector3 v in raysY)
        {
            rays.Add(v);
        }
        
        //Copy and rotate for x
        int linesToAddPerSideX = (xLines - 1) / 2;
        //Debug.Log("Lines to add per side: " + linesToAddPerSideX);
        
        for(int i = 1; i <= linesToAddPerSideX; i++)
        {
            float angle = (fovX/2.0f) * (((float)i) / ((float)linesToAddPerSideX));
            Quaternion rotationU = Quaternion.AngleAxis(angle, transform.right);
            Quaternion rotationD = Quaternion.AngleAxis(-angle, transform.right);
            
            foreach(Vector3 v in raysY)
            {
                Vector3 addUp = rotationU * v;
                Vector3 addDown = rotationD * v;
                rays.Add(addUp);
                rays.Add(addDown);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Continously visualize the sensor rays
        if(visualize)
        {
            foreach(Vector3 v in rays)
            {
                Debug.DrawRay(transform.position, v, Color.red, 0);
            }
        }
    }
    
    void FixedUpdate()
    {
        timeElapsed += Time.deltaTime;
        
        if(timeElapsed > publishDelay)
        {
            timeElapsed = 0;
            
            List<Vector3> points = new List<Vector3>();
            foreach(Vector3 v in rays)
            {
                RaycastHit hit;
                //A point is only added if the raycast actually hit anything
                if(Physics.Raycast(transform.position, v, out hit, v.magnitude))
                {
                    Vector3 hitpoint = hit.point;
                    //Visualize the hit point in the world
                    if(visualize)
                    {
                        Debug.DrawRay(hitpoint, Vector3.up, Color.green, 0);
                    }
                    
                    //We don't want the world coordinate of the hit point
                    //In real life, a lidar doesn't know the world coordinates
                    //only the coordinate relative to itself
                    //This is achieved simply by subtracting the current position
                    Vector3 sensorpoint = hitpoint - transform.position;
                    
                    //Add uncertainty
                    Vector3 uncertainPoint = (UnityEngine.Random.insideUnitSphere * maxuncertain) + sensorpoint;

                    points.Add(uncertainPoint);
                }
            }
            
            //Add all points in points to a ROS message and publish it
            PointCloud2Msg message = new PointCloud2Msg();
            
            //Construct the message. This is done exactly like the Blickfeld Cube Lidar does it
            //(the timestamps and seqs are neglected)
            HeaderMsg header = new HeaderMsg();
            header.frame_id = "lidar";
            
            message.header = header;
            
            message.height = (uint) 1;
            message.width =  (uint) points.Count;
            
            //field array is pre-constructed in Start()
            message.fields = new PointFieldMsg[4];
            fieldsDesc.CopyTo(message.fields, 0);
            
            message.is_bigendian = false;
            message.point_step = (uint) 16;
            message.row_step = (uint) (points.Count * 16);
            message.is_dense = false;
            
            //Setup the data array
            //This only needs to be a 32-bit float array. Intensity should be an int, but it's
            //always 0, so no special treatment is needed
            
            //First fill a list with the float values
            List<float> floats = new List<float>();
            
            foreach(Vector3 point in points)
            {
                //The Unity 3D frame and ROS 3D frame don't seem to line up...
                //x y and z values need to be swapped in places like this so they show up
                //correctly in rviz...
                floats.Add(point.x); //z is the x value
                floats.Add(point.z); //x is the z value
                floats.Add(point.y); //y is the y value
                
                //This is a dummy value for the intensity
                floats.Add(0.0f); //0.0f or 0, both are 0x00000000
            }
            
            //Convert the float-list to a float-array
            float[] floatarray = floats.ToArray();
            
            //Define a byte array and copy the floats to the byte array
            byte[] data = new byte[floatarray.Length * 4];
            Buffer.BlockCopy(floatarray, 0, data, 0, data.Length);
            
            //The byte array now contains the floats that describe the points
            //Put it into the ROS message
            message.data = data;
            
            ros.Publish(pubTopic, message);
        }
    }
}

