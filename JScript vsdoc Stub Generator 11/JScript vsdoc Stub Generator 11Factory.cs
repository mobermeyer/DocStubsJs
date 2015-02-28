using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace JScript_vsdoc_Stub_Generator_11
{
    #region Adornment Factory
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("JavaScript")]
    [ContentType("JScript")]
    [ContentType("HTML")]
    [ContentType("TypeScript")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class JScript_vsdoc_Stub_Generator_11Factory : IWpfTextViewCreationListener
    {
        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered 
        /// after the selection layer in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("JScript_vsdoc_Stub_Generator_11")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        public AdornmentLayerDefinition editorAdornmentLayer = null;
                
        /// <summary>
        /// Instantiates a JScript_vsdoc_Stub_Generator_11 manager when a textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        {
            new VSDocStubs(textView);
            new JSDocStubs(textView);
        }
    }
    #endregion //Adornment Factory

}
