using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using RosMessageTypes.Sensor;

public class CameraSimulator : MonoBehaviour
{
    public string _pubTopic;
    public float publishDelay;
    public RenderTexture _renderTexture;
    public ROSConnection ros;
    
    private Camera _simulationCamera;
    private float timeElapsed;
    
    void Start()
    {
        _simulationCamera = GetComponent<Camera>();
        // start the ROS connection
        ros.RegisterPublisher<ImageMsg>(_pubTopic);
        
        //Setup simulation camera
        //This camera will only be used to take snapshots and will actually never render to the screen
        //As such, adjustments must be made
        //Cameras that don't render to screen render into a RenderTexture instead.
        //Set render texture for the observation camera
        _simulationCamera.targetTexture = _renderTexture;
        //Force camera to only use render texture
        _simulationCamera.forceIntoRenderTexture = true;
    }
    
    void Update()
    {
        timeElapsed += Time.deltaTime;
        
        if(timeElapsed > publishDelay)
        {
            timeElapsed = 0;
            //Take snapshot
            
            //Make the observation camera render to its render texture
            _simulationCamera.Render();
            
            //The render texture of this camera is now filled with the pixels of this camera
            //To get the pixels, ReadPixels must be called
            //However, this function does not take the camera as an argument, but rather 
            //reads from the currently active render texture, which is null if the
            //main camera is rendering to screen (why??)
            //So, to get the pixels, the previous render texture (probably null) must 
            //be stored, the camera's texture must be activated, 
            //ReadPixels must be called and then the previous render texture must be restored.
            
            //Store current render texture (probably null)
            RenderTexture previousTexture = RenderTexture.active;
            
            //Set the active texture to the camera's render texture
            RenderTexture.active = _simulationCamera.targetTexture;
            
            //Now begin reading the pixels.
            
            //Make a new texture object for storage (this object must be manually destroyed later!)
            int height = _simulationCamera.targetTexture.height;
            int width = _simulationCamera.targetTexture.width;
            Texture2D image = new Texture2D(width, height);
            //Read the pixels
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            
            //Restore the previous render texture (probably null)
            RenderTexture.active = previousTexture;
            
            //Get the pixels (Color32 is better because 0-255 is better than 0.0f-1.0f)
            Color32[] pixels = image.GetPixels32();
            
            //Release the Texture2D object (THIS PREVENTS A MEMORY LEAK!)
            UnityEngine.Object.Destroy(image);
            
            //Now we have the pixels. Setup the ROS message.
            ImageMsg message = new ImageMsg();
            
            //Maybe add a header later?
            //message.header = todo
            message.height = (uint) height;
            message.width = (uint) width;
            message.encoding = "rgb8";
            message.is_bigendian = 0;
            message.step = (uint) (width * 3); //Size of a row is the number of columns aka the width.
            
            //Setup the data
            List<byte> data = new List<byte>();
            
            //Fill the data array
            
            //pixels are mirrored horizontally
            //the first row describes the last row and the last row describes the first row
            //so start with the last row and end with the first row
            //the array is flattened but we can still access it with 2D coordinates by converting
            //the index values
            for(int h = height - 1; h >= 0; h--)
            {
                for(int w = 0; w < width; w++)
                {
                    //convert 2d coordinates to 1d coordinates
                    int index = (h * width) + w;
                    Color32 color = pixels[index];
                    
                    data.Add(color.r);
                    data.Add(color.g);
                    data.Add(color.b);
                }
            }
            
            message.data = data.ToArray();
            
            ros.Publish(_pubTopic, message);
        }
    }
}
