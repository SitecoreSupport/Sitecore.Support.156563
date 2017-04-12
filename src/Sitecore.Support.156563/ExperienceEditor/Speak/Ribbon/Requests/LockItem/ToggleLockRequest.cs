using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Web;
using Sitecore.Workflows;
using System;
using System.Reflection;
using System.Web;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.LockItem
{
  public class ToggleLockRequest : PipelineProcessorRequest<ItemContext>
  {
    private static readonly BindingFlags _bFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private void HandleVersionCreating(Item finalItem)
    {
      if (base.RequestContext.Item.Version.Number != finalItem.Version.Number)
      {
        WebUtil.SetCookieValue(base.RequestContext.Site.GetCookieKey("sc_date"), string.Empty, DateTime.MinValue);
      }
    }

    public override PipelineProcessorResponseValue ProcessRequest()
    {
      base.RequestContext.ValidateContextItem();
      Item finalItem = this.SwitchLock(base.RequestContext.Item);
      this.HandleVersionCreating(finalItem);
      return new PipelineProcessorResponseValue
      {
        Value = new
        {
          Locked = finalItem.Locking.IsLocked(),
          Version = finalItem.Version.Number
        }
      };
    }

    protected Item SwitchLock(Item item)
    {
      if (item.Locking.IsLocked())
      {
        item.Locking.Unlock();
        return item;
      }
      if (Context.User.IsAdministrator)
      {
        item.Locking.Lock();
        return item;
      }
      return StartEditing(Context.Workflow, item);
    }

    protected virtual Item StartEditing(Sitecore.Workflows.WorkflowContext workflow, Item item)
    {
      Error.AssertObject(item, "item");
      if (!Settings.RequireLockBeforeEditing || Context.User.IsAdministrator)
      {
        return item;
      }

      var _contextField = workflow.GetType().GetField("_context", _bFlags);
      var _contextValue = (Sitecore.Context.ContextData)_contextField.GetValue(workflow);
      if (_contextValue.IsAdministrator)
      {
        return this.Lock(item);
      }
      if (StandardValuesManager.IsStandardValuesHolder(item))
      {
        return this.Lock(item);
      }
      if (!workflow.HasWorkflow(item) && !workflow.HasDefaultWorkflow(item))
      {
        return this.Lock(item);
      }
      if (!IsApproved(workflow, item))
      {
        return this.Lock(item);
      }
      Item item2 = item.Versions.AddVersion();
      if (item2 != null)
      {
        return this.Lock(item2);
      }
      return null;
    }

    protected virtual Item Lock(Item item)
    {
      if (TemplateManager.IsFieldPartOfTemplate(FieldIDs.Lock, item) && !item.Locking.Lock())
      {
        return null;
      }
      return item;
    }

    protected virtual bool IsApproved(Sitecore.Workflows.WorkflowContext workflow, Item item)
    {
      Error.AssertObject(item, "item");
      IWorkflow _workflow = GetWorkflow(workflow, item);
      if (_workflow != null)
      {
        return _workflow.IsApproved(item, null);
      }
      return true;
    }

    protected virtual IWorkflow GetWorkflow(Sitecore.Workflows.WorkflowContext workflow, Item item)
    {
      Error.AssertObject(item, "item");
      if (IsWorkflowEnabled(workflow))
      {
        IWorkflowProvider workflowProvider = item.Database.WorkflowProvider;
        if (workflowProvider != null)
        {
          return workflowProvider.GetWorkflow(item);
        }
      }
      return null;
    }

    protected virtual bool IsWorkflowEnabled(Sitecore.Workflows.WorkflowContext workflow)
    {
      switch (Switcher<WorkflowContextState, WorkflowContextState>.CurrentValue)
      {
        case WorkflowContextState.Default:
          {
            var _contextField = workflow.GetType().GetField("_context", _bFlags);
            var _contextValue = (Sitecore.Context.ContextData)_contextField.GetValue(workflow);
            return ((_contextValue.Site != null) && (_contextValue.Site.EnableWorkflow || IsReferrerInEditMode()));
          }
        case WorkflowContextState.Disabled:
          return false;
      }
      return true;
    }

    protected virtual bool IsReferrerInEditMode()
    {
      const string editModePattern = "mode=edit";
      const string urlEditModePattern = "sc_mode=edit";
      var referrer = HttpContext.Current.Request.UrlReferrer;
      if (referrer != null && !string.IsNullOrWhiteSpace(referrer.Query))
      {
        string innerQuery = null;
        bool res = IsInQuery(editModePattern, referrer.Query, out innerQuery);
        if (res)
        {
          return res;
        }
        else if (!res && !string.IsNullOrWhiteSpace(innerQuery))
        {
          string innerQueryStub = null;
          return IsInQuery(urlEditModePattern, innerQuery, out innerQueryStub);
        }
      }
      return false;
    }

    protected virtual bool IsInQuery(string param, string query, out string innerQuery)
    {
      innerQuery = null;
      var queryParts = query.Split(new char[] { '?', '&', '/' }, StringSplitOptions.RemoveEmptyEntries);
      string urlParam = string.Empty;
      foreach (string queryPart in queryParts)
      {
        string q = HttpContext.Current.Server.UrlDecode(queryPart);
        if (param.Equals(q, StringComparison.InvariantCultureIgnoreCase))
        {
          return true;
        }
        if (q.StartsWith("url=", StringComparison.InvariantCultureIgnoreCase))
        {
          innerQuery = q;
        }
      }
      return false;
    }
  }
}
