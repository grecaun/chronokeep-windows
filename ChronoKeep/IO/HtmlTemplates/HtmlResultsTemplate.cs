﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace ChronoKeep.IO.HtmlTemplates
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using ChronoKeep;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class HtmlResultsTemplate : HtmlResultsTemplateBase
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n");
            this.Write("\n<html>\n\t<head>\n\t\t<link rel=\'stylesheet\' href=\'bootstrap.css\'>\n\t\t<link rel=\'style" +
                    "sheet\' href=\'style.css\'>\n\t</head>\n\t<body>\n\t\t<div class=\'events-results-wrapper p" +
                    "anel-group\' id=\'results\'>\n\t\t\t<div class=\'text-important table-header\'><h2>");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.YearCode));
            
            #line default
            #line hidden
            this.Write(" ");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.Name));
            
            #line default
            #line hidden
            this.Write("</h2></div>\n\t\t\t<div class=\'events-panel panel panel-default\'>\n\t\t\t\t<div class=\'eve" +
                    "nts-btn-wrapper\'>\n\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 foreach (string d in distanceResults.Keys)
					{ 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t<button class=\"btn btn-default events-btn collapsed\" type=\"button\" data-tog" +
                    "gle=\"collapse\" data-parent=\"#results\" data-target=\"#");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.YearCode));
            
            #line default
            #line hidden
            this.Write("-results-");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d.Replace(' ', '-')));
            
            #line default
            #line hidden
            this.Write("\" aria-expanded=\"false\" aria-controls=\"");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.YearCode));
            
            #line default
            #line hidden
            this.Write("-results-");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d.Replace(' ', '-')));
            
            #line default
            #line hidden
            this.Write("\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("</button>\n\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t</div>\n\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 foreach (string d in distanceResults.Keys)
				{ 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t<div class=\"collapse\" id=\"");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.YearCode));
            
            #line default
            #line hidden
            this.Write("-results-");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d.Replace(' ', '-')));
            
            #line default
            #line hidden
            this.Write("\">\n\t\t\t\t\t<div class=\'well\'>\n\t\t\t\t\t\t<table>\n\t\t\t\t\t\t\t<tr><td colspan=\'8\' class=\'table-" +
                    "header text-important\'>");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write(@"</td></tr>
							<tr>
							   <td class=""overflow-hidden text-important table-label"">Place</td>
							   <td class=""overflow-hidden text-important table-label"">Age Place</td>
							   <td class=""text-important table-label"">First</td>
							   <td class=""text-important table-label"">Last</td>
							   <td class=""overflow-hidden text-important table-label"">Age</td>
							   <td class=""overflow-hidden text-important table-label"">Gender</td>
							   <td class=""overflow-hidden text-important table-label"">Gun Time</td>
							   <td class=""text-important table-label"">Time</td>
							</tr>
							");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 foreach (TimeResult r in distanceResults[d])
							{ 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t<tr>\n\t\t\t\t\t\t\t   <td class=\"overflow-hidden\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.PlaceStr));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t   <td class=\"overflow-hidden\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.AgePlaceStr));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t   <td>");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(participantDictionary[r.EventSpecificId].FirstName));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t   <td>");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(participantDictionary[r.EventSpecificId].LastName));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t   <td class=\"overflow-hidden\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(participantDictionary[r.EventSpecificId].Age(theEvent.Date)));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t   <td class=\"overflow-hidden\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(participantDictionary[r.EventSpecificId].Gender));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t   <td class=\"overflow-hidden\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Time.Substring(0, r.Time.Length > 3 ? r.Time.Length -2 : r.Time.Length)));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t   <td>");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.ChipTime.Substring(0, r.ChipTime.Length > 3 ? r.ChipTime.Length -2 : r.ChipTime.Length)));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t</tr>\n\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t</table>\n\t\t\t\t\t</div>\n\t\t\t\t</div>\n\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t</div>\n\t\t</div>\n\t\t<script type=\"text/javascript\" src=\'jquery.js\'></script>\n\t\t" +
                    "<script type=\"text/javascript\" src=\'bootstrap.js\'></script>\n\t</body>\n</html>");
            return this.GenerationEnvironment.ToString();
        }
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class HtmlResultsTemplateBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        protected System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}