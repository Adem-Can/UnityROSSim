using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;

public class RosSubscriberExample : MonoBehaviour
{
    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<StringMsg>("unityInput", subCallback);
    }

    void subCallback(StringMsg message)
    {
        string data = message.data;
        
        Debug.Log("Someone said something in ROS!");
        Debug.Log(data);
    }
}
