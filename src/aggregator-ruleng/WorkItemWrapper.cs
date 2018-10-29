﻿using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class WorkItemWrapper
    {
        private EngineContext _context;
        private WorkItem _item;
        private readonly JsonPatchDocument _changes = new JsonPatchDocument();
        private readonly bool _isReadOnly = false;

        internal WorkItemWrapper(EngineContext context, WorkItem item)
        {
            _context = context;
            _item = item;

            if (item.Id.HasValue)
            {
                Id = new PermanentWorkItemId(item.Id.Value);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Test,
                    Path = "/rev",
                    Value = item.Rev
                });
                _context.Tracker.TrackExisting(this);
            }
            else
            {
                Id = new TemporaryWorkItemId(_context.Tracker);
                _context.Tracker.TrackNew(this);
            }
        }

        public WorkItemWrapper(EngineContext context, string project, string type)
        {
            _context = context;

            Id = new TemporaryWorkItemId(_context.Tracker);

            _item = new WorkItem();
            _item.Fields[CoreFieldRefNames.TeamProject] = project;
            _item.Fields[CoreFieldRefNames.WorkItemType] = type;
            _item.Fields[CoreFieldRefNames.Id] = Id.Value;

            _context.Tracker.TrackNew(this);
        }

        public WorkItemWrapper(EngineContext context, WorkItemWrapper template, string type)
        {
            _context = context;

            Id = new TemporaryWorkItemId(_context.Tracker);

            _item = new WorkItem();
            _item.Fields[CoreFieldRefNames.TeamProject] = template.TeamProject;
            _item.Fields[CoreFieldRefNames.WorkItemType] = type;
            _item.Fields[CoreFieldRefNames.Id] = Id.Value;

            _context.Tracker.TrackNew(this);
        }

        internal WorkItemWrapper(EngineContext context, WorkItem item, bool isReadOnly)
            // we cannot reuse the code, because tracking is different
            //: this(context, item)
        {
            _context = context;

            Id = new PermanentWorkItemId(item.Id.Value);
            Changes.Add(new JsonPatchOperation()
            {
                Operation = Operation.Test,
                Path = "/rev",
                Value = item.Rev
            });
            _isReadOnly = isReadOnly;
            _item = item;
            _context.Tracker.TrackRevision(this);
        }

        public WorkItemWrapper PreviousRevision
        {
            get
            {
                if (Rev > 0)
                {
                    // TODO we shouldn't use the client in this class
                    // TODO check if already loaded
                    var previousRevision = _context.Client.GetRevisionAsync(this.Id.Value, this.Rev - 1, expand: WorkItemExpand.All).Result;
                    return new WorkItemWrapper(_context, previousRevision, true);
                }

                return null;
            }
        }

        public IEnumerable<WorkItemWrapper> Revisions
        {
            get
            {
                // TODO load a few revisions at a time
                //var all = _context.Client.GetRevisionsAsync(this.Id.Value, expand: WorkItemExpand.All).Result;
                var revision = this;
                while ((revision = revision.PreviousRevision) != null)
                {
                    yield return revision;
                }
            }
        }

        public IEnumerable<WorkItemRelationWrapper> Relations
        {
            get
            {
                return new WorkItemRelationWrapperCollection(this, _item.Relations);
            }
        }

        public IEnumerable<WorkItemRelationWrapper> ChildrenLinks
        {
            get
            {
                return new WorkItemRelationWrapperCollection(this, _item.Relations)
                    .Where(rel => rel.Rel == CoreRelationRefNames.Children);
            }
        }

        public IEnumerable<WorkItemWrapper> Children
        {
            get
            {
                var store = new WorkItemStore(_context);
                return store.GetWorkItems(ChildrenLinks);
            }
        }

        public IEnumerable<WorkItemRelationWrapper> RelatedLinks
        {
            get
            {
                return new WorkItemRelationWrapperCollection(this, _item.Relations)
                    .Where(rel => rel.Rel == CoreRelationRefNames.Related);
            }
        }

        public IEnumerable<WorkItemRelationWrapper> Hyperlinks
        {
            get
            {
                return new WorkItemRelationWrapperCollection(this, _item.Relations)
                    .Where(rel => rel.Rel == CoreRelationRefNames.Hyperlink);
            }
        }

        public WorkItemRelationWrapper ParentLink
        {
            get
            {
                return new WorkItemRelationWrapperCollection(this, _item.Relations)
                    .Where(rel => rel.Rel == CoreRelationRefNames.Parent)
                    .SingleOrDefault();
            }
        }

        public WorkItemWrapper Parent
        {
            get
            {
                var store = new WorkItemStore(_context);
                return store.GetWorkItem(ParentLink);
            }
        }

        public void AddChild(WorkItemWrapper newChild)
        {
            var rel = new WorkItemRelation { Url = newChild.Url, Rel = CoreRelationRefNames.Children };
            _item.Relations.Add(rel);
            var rels = new WorkItemRelationWrapperCollection(this, _item.Relations);
            rels.Add(new WorkItemRelationWrapper(this, rel));
        }

        public WorkItemId<int> Id
        {
            get;
        }

        public int Rev
        {
            get { return _item.Rev.Value; }
        }

        public string Url
        {
            get { return _item.Url; }
        }

        public string WorkItemType
        {
            get { return (string)_item.Fields[CoreFieldRefNames.WorkItemType]; }
            private set { SetFieldValue(CoreFieldRefNames.WorkItemType, value); }
        }

        public string State
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.State); }
            set { SetFieldValue(CoreFieldRefNames.State, value); }
        }

        public int AreaId
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.AreaId); }
            set { SetFieldValue(CoreFieldRefNames.AreaId, value); }
        }

        public string AreaPath
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.AreaPath); }
            set { SetFieldValue(CoreFieldRefNames.AreaPath, value); }
        }

        public string AssignedTo
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.AssignedTo); }
            set { SetFieldValue(CoreFieldRefNames.AssignedTo, value); }
        }

        public int AttachedFileCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.AttachedFileCount); }
            set { SetFieldValue(CoreFieldRefNames.AttachedFileCount, value); }
        }

        public string AuthorizedAs
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.AuthorizedAs); }
            set { SetFieldValue(CoreFieldRefNames.AuthorizedAs, value); }
        }

        public string ChangedBy
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.ChangedBy); }
            set { SetFieldValue(CoreFieldRefNames.ChangedBy, value); }
        }

        public DateTime? ChangedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.ChangedDate); }
            set { SetFieldValue(CoreFieldRefNames.ChangedDate, value); }
        }

        public string CreatedBy
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.CreatedBy); }
            set { SetFieldValue(CoreFieldRefNames.CreatedBy, value); }
        }

        public DateTime? CreatedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.CreatedDate); }
            set { SetFieldValue(CoreFieldRefNames.CreatedDate, value); }
        }

        public string Description
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Description); }
            set { SetFieldValue(CoreFieldRefNames.Description, value); }
        }

        public int ExternalLinkCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.ExternalLinkCount); }
            set { SetFieldValue(CoreFieldRefNames.ExternalLinkCount, value); }
        }

        public string History
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.History); }
            set { SetFieldValue(CoreFieldRefNames.History, value); }
        }

        public int HyperLinkCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.HyperLinkCount); }
            set { SetFieldValue(CoreFieldRefNames.HyperLinkCount, value); }
        }

        public int IterationId
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.IterationId); }
            set { SetFieldValue(CoreFieldRefNames.IterationId, value); }
        }

        public string IterationPath
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.IterationPath); }
            set { SetFieldValue(CoreFieldRefNames.IterationPath, value); }
        }

        public string Reason
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Reason); }
            set { SetFieldValue(CoreFieldRefNames.Reason, value); }
        }

        public int RelatedLinkCount
        {
            get { return GetFieldValue<int>(CoreFieldRefNames.RelatedLinkCount); }
            set { SetFieldValue(CoreFieldRefNames.RelatedLinkCount, value); }
        }

        public DateTime? RevisedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.RevisedDate); }
            set { SetFieldValue(CoreFieldRefNames.RevisedDate, value); }
        }

        public DateTime? AuthorizedDate
        {
            get { return GetFieldValue<DateTime?>(CoreFieldRefNames.AuthorizedDate); }
            set { SetFieldValue(CoreFieldRefNames.AuthorizedDate, value); }
        }

        public string TeamProject
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.TeamProject); }
            set { SetFieldValue(CoreFieldRefNames.TeamProject, value); }
        }

        public string Tags
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Tags); }
            set { SetFieldValue(CoreFieldRefNames.Tags, value); }
        }

        public string Title
        {
            get { return GetFieldValue<string>(CoreFieldRefNames.Title); }
            set { SetFieldValue(CoreFieldRefNames.Title, value); }
        }

        public double Watermark
        {
            get { return GetFieldValue<double>(CoreFieldRefNames.Watermark); }
            set { SetFieldValue(CoreFieldRefNames.Watermark, value); }
        }

        public bool IsDeleted
        {
            get { return GetFieldValue<bool>(CoreFieldRefNames.IsDeleted); }
            set { SetFieldValue(CoreFieldRefNames.IsDeleted, value); }
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
        }

        public bool IsNew => Id is TemporaryWorkItemId;

        public bool IsDirty { get; internal set; }

        internal JsonPatchDocument Changes
        {
            get { return _changes; }
        }

        public object this[string field]
        {
            get { return GetFieldValue<object>(field); }
            set { SetFieldValue(field, value); }
        }

        private void SetFieldValue(string field, object value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            if (_item.Fields.ContainsKey(field))
            {
                _item.Fields[field] = value;
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Replace,
                    Path = "/fields/" + field,
                    Value = value
                });
            }
            else
            {
                _item.Fields.Add(field, value);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + field,
                    Value = value
                });
            }

            IsDirty = true;
        }

        private T GetFieldValue<T>(string field)
        {
            return _item.Fields.ContainsKey(field)
                ? (T)_item.Fields[field]
                : default(T);
        }
    }
}
