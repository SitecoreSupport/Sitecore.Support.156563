using Sitecore.Data.Items;
using Sitecore.Events;
using System;

namespace Sitecore.Support.ExperienceEditor.LockAndEdit
{
  public class EnsureWorkflowState
  {
    private const string itemLockPath = "/-/speak/request/v1/expeditor/ExperienceEditor.LockItem";
    protected void OnItemVersionCreated(object sender, System.EventArgs args)
    {
      Item item = null;
      if (IsAbsolutePathEqual() && IsArgsValid(args, out item))
      {
        if (!string.IsNullOrWhiteSpace(item[Sitecore.FieldIDs.Workflow]))
        {
          if (string.IsNullOrWhiteSpace(item[Sitecore.FieldIDs.WorkflowState]))
          {
            var workflowState = item.Database.GetItem(item[Sitecore.FieldIDs.Workflow]);
            if (workflowState != null)
            {
              string initialState = workflowState["Initial state"];
              if (!string.IsNullOrWhiteSpace(initialState))
              {
                using (new EditContext(item, false, true))
                {
                  item[Sitecore.FieldIDs.WorkflowState] = initialState;
                }
              }
            }
          }
        }
      }
    }

    protected virtual bool IsAbsolutePathEqual()
    {
      var context = System.Web.HttpContext.Current;
      if (context != null && context.Request != null && context.Request.Url != null)
      {
        string path = context.Request.Url.AbsolutePath;
        if (path != null)
        {
          return path.Equals(itemLockPath, StringComparison.InvariantCultureIgnoreCase);
        }
      }
      return false;
    }

    protected virtual bool IsArgsValid(System.EventArgs args, out Item item)
    {
      item = null;
      if (args != null && args is SitecoreEventArgs)
      {
        var explArgs = args as SitecoreEventArgs;
        if (explArgs.Parameters != null && explArgs.Parameters.Length > 0)
        {
          item = explArgs.Parameters[0] as Item;
          if (item != null && item.Database != null)
          {
            return true;
          }
        }
      }
      return false;
    }
  }
}