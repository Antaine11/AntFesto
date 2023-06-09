﻿// Game4Automation (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 in2Sight GmbH - Usage of this source code only allowed based on License conditions see https://game4automation.com/lizenz  

using UnityEngine;


namespace game4automation
{

    public class DemoReadNodeNotRecommended : MonoBehaviour
    {

        public OPCUA_Interface Interface;
        public string NodeId;

        // Update is called once per frame
        void Update()
        {

            float myvar = (float) Interface.ReadNodeValue(NodeId);
        }
    }
}