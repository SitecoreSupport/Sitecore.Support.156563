using Sitecore.Data.Items;
using System;

namespace Sitecore.Support.ExperienceEditor.LockAndEdit
{
  public class EnsureWorkflowState
  {
    private const string itemLockPath = "/-/speak/request/v1/expeditor/ExperienceEditor.LockItem";
    protected void OnItemVersionCreated(object sender, System.EventArgs args)
    {
      if (System.Web.HttpContext.Current.Request.Url.AbsolutePath.ToLowerInvariant().Equals(itemLockPath, StringComparison.InvariantCultureIgnoreCase))
      {
        if (args != null && args is Sitecore.Events.SitecoreEventArgs)
        {
          var explArgs = args as Sitecore.Events.SitecoreEventArgs;
          if (explArgs.Parameters.Length > 0)
          {
            var item = explArgs.Parameters[0] as Sitecore.Data.Items.Item;
            if (item != null && !string.IsNullOrWhiteSpace(item[Sitecore.FieldIDs.Workflow]))
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
      }
    }
  }
}