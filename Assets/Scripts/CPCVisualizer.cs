using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.IlmRosPkg;
using RosMessageTypes.Sensor;

public class CPCVisualizer : MonoBehaviour
{
    public float length;
    public float duration;
    public int camno = 1;
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<ColoredPointcloudMsg>("/protofuse/result", subCallback);
    }

    void subCallback(ColoredPointcloudMsg message)
    {   
        //Debug.Log("Someone said something in ROS!");
        
        PointCloud2Msg pointcloud = message.pointCloud;
        
        int numberOfPoints = (int)(pointcloud.height * pointcloud.width);
        //Debug.Log("There are " + numberOfPoints + " points");
        
        byte[] byteArray = pointcloud.data;
        //Debug.Log("There are " + byteArray.Length + " bytes");
        
        float[] points = new float[byteArray.Length / 4];
        //Debug.Log("There are " + points.Length + " point values");
        
        Buffer.BlockCopy(byteArray, 0, points, 0, byteArray.Length);
        
        ColorArrayMsg array1 = message.colorscam1;
        ColorArrayMsg array2 = message.colorscam2;
        
        int element = 0;
        for(int i = 0; i < points.Length; i += 4)
        {
            //These are in ROS coordinates
            float x = points[i + 0];
            float y = points[i + 1];
            float z = points[i + 2];
            //points[i+3] is a 32-bit float for intensity that we don't care about
            
            //convert ROS to Unity
            Vector3 point = new Vector3(x, z, y);
            
            ColorMsg colormsg;
            
            if(camno == 1)
            {
                colormsg = array1.colors[element];
            }
            else
            {
                colormsg = array2.colors[element];
            }
            
            Color32 color = new Color32(colormsg.red, colormsg.green, colormsg.blue, 255);
            
            
            Vector3 xm = new Vector3(point.x - length, point.y, point.z);
            Vector3 xp = new Vector3(point.x + length, point.y, point.z);
            Debug.DrawLine(xm, xp, color, duration);
            Vector3 ym = new Vector3(point.x, point.y - length, point.z);
            Vector3 yp = new Vector3(point.x, point.y + length, point.z);
            Debug.DrawLine(ym, yp, color, duration);
            Vector3 zm = new Vector3(point.x, point.y, point.z - length);
            Vector3 zp = new Vector3(point.x, point.y, point.z + length);
            Debug.DrawLine(zm, zp, color, duration);
            element++;
            
        }
        
    }
}
