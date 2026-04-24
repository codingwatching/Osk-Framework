using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public abstract class ProcedureNode 
    {
        /// <summary>
        /// Called when the node is initialized.
        /// </summary>
        public virtual void OnInit(ProcedureProcessor processor) {}

        /// <summary>
        /// Called when the node becomes active.
        /// </summary>
        public virtual void OnEnter(ProcedureProcessor processor) {}

        /// <summary>
        /// Called continuously while the node is active.
        /// </summary>
        public virtual void OnUpdate(ProcedureProcessor processor) {}
        
        /// <summary>
        /// called continuously at a fixed interval.
        ///  </summary>
        public virtual void OnFixedUpdate(ProcedureProcessor processor) {}
        
        /// <summary>
        /// called continuously at a fixed interval.
        ///  </summary>
        public virtual void OnLateUpdate(ProcedureProcessor processor) {}

        /// <summary>
        /// Called when the node is exited.
        /// </summary>
        public virtual void OnExit(ProcedureProcessor processor) {}

        /// <summary>
        /// Called when the node is removed from the processor.
        /// </summary>
        public virtual void OnRemove(ProcedureProcessor processor) {}

        /// <summary>
        /// Switch to another ProcedureNode.
        /// </summary>
        protected void ChangeState<T>(ProcedureProcessor processor) where T : ProcedureNode
        {
            processor.ChangeNode(typeof(T));
        }

        /// <summary>
        /// Switch to another ProcedureNode by type.
        /// </summary>
        protected void ChangeState(ProcedureProcessor processor, Type stateType)
        {
            processor.ChangeNode(stateType);
        }
    }
}
