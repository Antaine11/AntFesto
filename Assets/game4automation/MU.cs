﻿// Game4Automation (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 in2Sight GmbH - Usage of this source code only allowed based on License conditions see https://game4automation.com/lizenz  

using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace game4automation
{
    [System.Serializable]
    public class Game4AutomationEventMU: UnityEvent<MU,bool>
    {
    }
    
    [System.Serializable]
    public class Game4AutomationEventMUDelete: UnityEvent<MU>
    {
    }
    
   
    
    
    [SelectionBase]
    //! Base class for free movable Unity (MU). MUs can be picked or loaded and they can be placed on Transport Surfaces. MUs are created by a Source and deleted by a Sink.
    [HelpURL("https://game4automation.com/documentation/current/mu.html")]
    public class MU : Game4AutomationBehavior
    {
        #region Public Attributes
        [ReadOnly] public int ID; //!<  ID of this MU (increases on each creation on the MU source
        [ReadOnly] public int GlobalID; //!< Global ID, increases for each MU independent on the source
        [ReorderableList] public List<GameObject> MUAppearences; //!< List of MU appearances for PartChanger
        [ReadOnly] public GameObject FixedBy; //!< Current Gripper which is picking the part
        [ReadOnly] public GameObject LoadedOn; //!< Current Part the MU is loaded on
        [ReadOnly] public GameObject StandardParent; //!< The standard parent Gameobject of the MU
        [ReadOnly] public GameObject ParentBeforeFix; //!< The parent of the MU before the last Grip
        [ReadOnly] public List<Sensor> CollidedWithSensors; //!< The current Sensors the Part is colliding with
        [ReadOnly] public List<MU> LoadedMus; //!< List of MUs whcih are loaded on this MU
        public float SurfaceAlignSmoothment = 50f;

        [HideInInspector]
        public float FixerLastDistance;
        [HideInInspector] public Rigidbody Rigidbody;
        [HideInInspector]public List<TransportSurface> AlignWithSurface = new List<TransportSurface>();
        [HideInInspector]public List<TransportSurface> TransportSurfaces = new List<TransportSurface>();
        [HideInInspector]public float DissolveDuration=0.5f;
        [HideInInspector]public float MaxDissolveValue=0.2f;

        [ReadOnly] public float Velocity;
        #endregion
        
        private Vector3 lastDirection;
        private List<Material> materials = new List<Material>();
        
     
        // Deletes all MUs which are loaded on MU as Subcomponent 
        // (but not RigidBodies which are standing on this MU)
        #region Events
        [Foldout("Events")] public Game4AutomationEventMUDelete EventMUDeleted;  //!< Event is called when MU is Deleted / Destroyed
        [Foldout("Events")] public Game4AutomationEventMU EventMUIsLoaded; //!< Event is called when MU is loaded onto another
        [Foldout("Events")] public Game4AutomationEventMU EventMUGetsLoad; //!< Event is called when MU gets a MU loaded onto itself
        [Foldout("Events")] public Game4AutomationEventMUSensor EventMUSensor;  //!< Event is called when MU collides with a Sensor
        #endregion
        
        
        #region Public Methods

        //  Places the part with the Bottom on top of the defined position
        public void PlaceMUOnTopOfPosition(Vector3 position)
        {
            Bounds bounds = new Bounds(transform.position,new Vector3(0,0,0));
            
            // Calculate Bounds
         
            Renderer[] renderers = GetComponentsInChildren<Renderer> ();
            foreach (Renderer renderer in renderers)
            {
                   bounds.Encapsulate (renderer.bounds);
            }
            
            // get bottom center
            var center = new Vector3(bounds.min.x+bounds.extents.x,bounds.min.y,bounds.min.z+bounds.extents.z);
            
            // get distance from center to bounds
            var distance = transform.position - center;

            transform.position = position + distance;
        }
        
        //! Load the named MU on this mu
        public void LoadMu(MU mu)
        {
            mu.transform.SetParent(this.transform);
            mu.EventMuLoad();
            LoadedMus.Add(mu);
            EventMUGetsLoad.Invoke(this,true);
        }
        
        //! Event that this called when MU enters sensor
        public void EventMUEnterSensor(Sensor sensor)
        {
            CollidedWithSensors.Add(sensor);
            EventMUSensor.Invoke(this,true);
        }
        
        //! Event that this called when MU enters sensor
        public void EventMUExitSensor(Sensor sensor)
        {
            CollidedWithSensors.Remove(sensor);
            EventMUSensor.Invoke(this,false);
        }


        //! Event that this MU is loaded onto another
        public void EventMuLoad()
        {
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;
            LoadedOn = transform.parent.gameObject;
            EventMUIsLoaded.Invoke(this,true);
        }

        //! Event that this MU is unloaded from another
        public void EventMUUnLoad()
        {
            EventMUIsLoaded.Invoke(this,false);
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;
            transform.parent = StandardParent.transform;
            LoadedOn = null;
            Rigidbody.WakeUp();
        }

        //  Init the MU wi MUName and IDs
        public void InitMu(string muname, int localid, int globalid)
        {
            ID = localid;
            GlobalID = globalid;
            name = muname;
            if (transform.parent != null)
            {
                StandardParent = transform.parent.gameObject;
            }
            else
            {
                StandardParent = transform.root.gameObject;
            }
        }
        
        //! Event that this MU is on Path
        public void EventMuEnterPathSimulation()
        {
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = false;
        }

        //! Event that this MU is unloaded from Path
        public void EventMUExitPathSimulation()
        {
            Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;
            Rigidbody.WakeUp();
        }

        // Public method for fixing MU to a gameobject
        public void Fix(GameObject fixto)
        {
            if (FixedBy != null)
            {
                var fix = FixedBy.GetComponent<IFix>();
                fix.Unfix(this);
            }
            else
            {
                if (this.transform.parent!=null)
                     ParentBeforeFix = this.transform.parent.gameObject;
            } 
            transform.SetParent(fixto.transform);
            Rigidbody.isKinematic = true;
            FixedBy = fixto;
        }

        // Public method for unfixing MU to a gameobject, parent changes are done based on parent before fix
        public void Unfix()
        {
            if (ParentBeforeFix != null)
                transform.SetParent(ParentBeforeFix.transform);
            else
                transform.SetParent(null);
            ParentBeforeFix = null;
            Rigidbody.isKinematic = false;
            Rigidbody.WakeUp();
            FixedBy = null;
        }

        //! Unloads one of the MUs which are loaded on this MU
        public void UnloadOneMu(MU mu)
        {
            EventMUGetsLoad.Invoke(this,false);
            mu.EventMUUnLoad();
            LoadedMus.Remove(mu);
        }

        //! Unloads all  of the MUs which are loaded on this MU
        public void UnloadAllMUs()
        {
            var tmploaded = LoadedMus.ToArray();
            foreach (var mu in tmploaded)
            {
                UnloadOneMu(mu);
            }
        }
        
        //! Slowly dissolves MU and destroys it
        public void Dissolve(float duration)
        {
            DissolveDuration = duration;
            if (DissolveDuration>0)
               StartCoroutine(DissolveCoroutine());
            Invoke("Destroy",DissolveDuration);
        }

        public void Appear(float duration)
        {
            DissolveDuration = duration;
            if (duration>0)
                  StartCoroutine(AppearCoroutine());
        
        }
        
        #endregion

        IEnumerator AppearCoroutine ()
        {
            float dissolveValue = MaxDissolveValue;
            var duration = DissolveDuration / Time.timeScale;
            while (dissolveValue > 0)
            {
                dissolveValue -= 0.01f;
                foreach (Material mat in materials)
                {
                    mat.SetFloat("_DissolveAmount",dissolveValue);
                    yield return null;
                }
                yield return new WaitForSeconds(duration/100f);
            }
        }
        
        IEnumerator DissolveCoroutine ()
        {
            float dissolveValue = 0f;
            var duration = DissolveDuration / Time.timeScale;
            while (dissolveValue < MaxDissolveValue)
            {
                dissolveValue += 0.01f;
                foreach (Material mat in materials)
                {
                    mat.SetFloat("_DissolveAmount",dissolveValue);
                    yield return null;
                }
                yield return new WaitForSeconds(DissolveDuration/100f);
            }
        }

   

        private void Destroy()
        {
            Destroy(this.gameObject);
        }
        
        private void OnDestroy()
        {
            if (CollidedWithSensors!=null)
            foreach (var sensor in CollidedWithSensors.ToArray())
            {
                sensor.OnMUDelete(this);
            }
            if (EventMUDeleted!=null)
                EventMUDeleted.Invoke(this);
        }
        
        private void Start()
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in rends)
            {
                materials.Add(rend.material);
            }
            Rigidbody = GetComponentInChildren<Rigidbody>();
            MaxDissolveValue = 0.3f;
        }
        
        public void FixedUpdate ()
        {
            
            if (AlignWithSurface.Count > 0) // Only align if fully on one surface
            {
                var surface = AlignWithSurface[0];
               
                var destrot = Quaternion.FromToRotation(transform.up, surface.transform.up )* Rigidbody.rotation;
                Rigidbody.rotation = Quaternion.Lerp(Rigidbody.rotation, destrot, SurfaceAlignSmoothment* Time.fixedDeltaTime);
            }
           
            
        }
    }
}