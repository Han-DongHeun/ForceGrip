using System;
using System.Collections.Generic;
using System.Linq;

public class ActionListener : IComparable<ActionListener>
{
    public Action action;
    public ushort order;
    
    /// <summary>
    /// Initializes a new instance of the Listener class.
    /// </summary>
    /// <param name="action">The action to be invoked.</param>
    /// <param name="order">The order in which the action should be invoked.</param>
    public ActionListener(Action action, ushort order)
    {
        this.action = action;
        this.order = order;
    }
    
    public int CompareTo(ActionListener other)
    {
        return order.CompareTo(other.order);
    }
    
    public static void AddListener(List<ActionListener> listeners, Action action, ushort? order = null)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        
        if (!order.HasValue)
        {
            order = (listeners.Count > 0) ? (ushort)(listeners.Max(l => l.order) + 1) : (ushort)0;
        }
        listeners.Add(new ActionListener(action, order.Value));
        listeners.Sort();
    }
}

public class ActionListener<T> : ActionListener
{
    public new Action<T> action;
    
    /// <summary>
    /// Initializes a new instance of the Listener class.
    /// </summary>
    /// <param name="action">The action to be invoked.</param>
    /// <param name="order">The order in which the action should be invoked.</param>
    public ActionListener(Action<T> action, ushort order) : base(null, order)
    {
        this.action = action;
    }
    
    public static void AddListener(List<ActionListener<T>> listeners, Action<T> action, ushort? order = null)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
        
        if (!order.HasValue)
        {
            order = (listeners.Count > 0) ? (ushort)(listeners.Max(l => l.order) + 1) : (ushort)0;
        }
        listeners.Add(new ActionListener<T>(action, order.Value));
        listeners.Sort();
    }
}