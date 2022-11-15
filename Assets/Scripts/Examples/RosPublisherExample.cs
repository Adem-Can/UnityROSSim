using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;


/// <summary>
///
/// </summary>
public class RosPublisherExample : MonoBehaviour
{
    //ROS Handler object
    ROSConnection ros;
    
    public string topicName = "unityChatter";
    
    public float publishMessageFrequency = 0.5f;

    private float timeElapsed;

    void Start()
    {
        // start the ROS connection
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<StringMsg>(topicName);
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;

        if (timeElapsed > publishMessageFrequency)
        {
            
            StringMsg message = new StringMsg("Hello! This was sent from Unity!");

            // Finally send the message to server_endpoint.py running in ROS
            ros.Publish(topicName, message);

            timeElapsed = 0;
        }
    }
}
