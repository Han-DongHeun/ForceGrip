using System;
using System.Collections.Generic;
using UnityEngine;

public class CollisionCallback: MonoBehaviour
{       
    private List<ActionListener<Collider>> onTriggerEnterListeners = new List<ActionListener<Collider>>();
    private List<ActionListener<Collider>> onTriggerExitListeners = new List<ActionListener<Collider>>();
    private List<ActionListener<Collider>> onTriggerStayListeners = new List<ActionListener<Collider>>();
    private List<ActionListener<Collision>> onCollisionEnterListeners = new List<ActionListener<Collision>>();
    private List<ActionListener<Collision>> onCollisionExitListeners = new List<ActionListener<Collision>>();
    private List<ActionListener<Collision>> onCollisionStayListeners = new List<ActionListener<Collision>>();
    
    public void AddOnTriggerEnterListener(Action<Collider> action, ushort? order = null)
    {
        ActionListener<Collider>.AddListener(onTriggerEnterListeners, action, order);
    }
    
    public void AddOnTriggerExitListener(Action<Collider> action, ushort? order = null)
    {
        ActionListener<Collider>.AddListener(onTriggerExitListeners, action, order);
    }
    
    public void AddOnTriggerStayListener(Action<Collider> action, ushort? order = null)
    {
        ActionListener<Collider>.AddListener(onTriggerStayListeners, action, order);
    }
    
    public void AddOnCollisionEnterListener(Action<Collision> action, ushort? order = null)
    {
        ActionListener<Collision>.AddListener(onCollisionEnterListeners, action, order);
    }
    
    public void AddOnCollisionExitListener(Action<Collision> action, ushort? order = null)
    {
        ActionListener<Collision>.AddListener(onCollisionExitListeners, action, order);
    }
    
    public void AddOnCollisionStayListener(Action<Collision> action, ushort? order = null)
    {
        ActionListener<Collision>.AddListener(onCollisionStayListeners, action, order);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        foreach (var listener in onTriggerEnterListeners)
        {
            listener.action(other);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        foreach (var listener in onTriggerExitListeners)
        {
            listener.action(other);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        foreach (var listener in onTriggerStayListeners)
        {
            listener.action(other);
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        foreach (var listener in onCollisionEnterListeners)
        {
            listener.action(collision);
        }
    }
    
    private void OnCollisionExit(Collision collision)
    {
        foreach (var listener in onCollisionExitListeners)
        {
            listener.action(collision);
        }
    }
    
    private void OnCollisionStay(Collision collision)
    {
        foreach (var listener in onCollisionStayListeners)
        {
            listener.action(collision);
        }
    }
    
    public void RemoveOnTriggerEnterListener(Action<Collider> action)
    {
        onTriggerEnterListeners.RemoveAll(l => l.action == action);
    }
    
    public void RemoveOnTriggerExitListener(Action<Collider> action)
    {
        onTriggerExitListeners.RemoveAll(l => l.action == action);
    }
    
    public void RemoveOnTriggerStayListener(Action<Collider> action)
    {
        onTriggerStayListeners.RemoveAll(l => l.action == action);
    }
    
    public void RemoveOnCollisionEnterListener(Action<Collision> action)
    {
        onCollisionEnterListeners.RemoveAll(l => l.action == action);
    }
    
    public void RemoveOnCollisionExitListener(Action<Collision> action)
    {
        onCollisionExitListeners.RemoveAll(l => l.action == action);
    }
    
    public void RemoveOnCollisionStayListener(Action<Collision> action)
    {
        onCollisionStayListeners.RemoveAll(l => l.action == action);
    }
    
    public void RemoveAllOnTriggerEnterListeners()
    {
        onTriggerEnterListeners.Clear();
    }
    
    public void RemoveAllOnTriggerExitListeners()
    {
        onTriggerExitListeners.Clear();
    }
    
    public void RemoveAllOnTriggerStayListeners()
    {
        onTriggerStayListeners.Clear();
    }
    
    public void RemoveAllOnCollisionEnterListeners()
    {
        onCollisionEnterListeners.Clear();
    }
    
    public void RemoveAllOnCollisionExitListeners()
    {
        onCollisionExitListeners.Clear();
    }
    
    public void RemoveAllOnCollisionStayListeners()
    {
        onCollisionStayListeners.Clear();
    }
    
    public void RemoveAllListeners()
    {
        RemoveAllOnTriggerEnterListeners();
        RemoveAllOnTriggerExitListeners();
        RemoveAllOnTriggerStayListeners();
        RemoveAllOnCollisionEnterListeners();
        RemoveAllOnCollisionExitListeners();
        RemoveAllOnCollisionStayListeners();
    }
}