﻿using  System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Melville.MVVM.BusinessObjects;

namespace Melville.MVVM.AdvancedLists.ListMonitors
{
  public interface IParent<TChild>
  {
    void LosingChild(TChild child);
  }
  public interface IChild<TParent> where TParent:notnull
  {
    /// <summary>
    ///   Gets or Sets the Parent
    /// </summary>
    TParent Parent { get; set; }
  }

  public class NotifyChildBase<T> : NotifyBase, IChild<T> where T:class
  {
    [MaybeNull]
    private T parent = null!;
    [MaybeNull]
    public T Parent
    {
      get => parent!;
      set => AssignAndNotify(ref parent!, value);
    }
  }


  public sealed class ParentListMonitor<TChild, TParent> : IListMonitor<TChild>
    where TChild : IChild<TParent> where TParent : class, IParent<TChild>
  {
    private readonly TParent parent;
    public ParentListMonitor(TParent parent)
    {
      this.parent = parent;
    }
    private void ClaimChild(TChild item)
    {
      if (item == null) return;
      if (item.Parent is IParent<TChild> parentToNotify &&
          parentToNotify != parent) // sometimes a child needs to claim its parent early -- do not remove just to put back in.
      {
        parentToNotify.LosingChild(item);
      }
      item.Parent = parent;
    }
    private static void DisownChild(TChild item)
    {
      if (item == null) return;

      item.Parent = default!;
    }
    public void NewItem(TChild item, int position)
    {
      ClaimChild(item);
    }
    public void DestroyItem(TChild item, int position)
    {
      DisownChild(item);
    }
    public void Move(int oldPosition, int newPosition, int length)
    {
      // I couldn't care less -- move all you want to
    }
    public void Reset()
    {
      // reordering is not a concern, clear will be routed through the
      // delete method
    }
    public void Initalize(IList<TChild> collection)
    {
      // do nothing
    }
  }
  public class ParentList<TItem, TParent> : ThreadSafeBindableCollection<TItem>
    where TItem : IChild<TParent> where TParent : class, IParent<TItem>
  {
    public ParentList(TParent parent)
    {
      new ParentListMonitor<TItem, TParent>(parent).AttachToList(this);
    }
  }
}