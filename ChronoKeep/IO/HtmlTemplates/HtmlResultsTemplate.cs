﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version: 17.0.0.0
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------
namespace Chronokeep.IO.HtmlTemplates
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using Chronokeep;
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
            this.Write("\n<!doctype html>\n<html lang=\"en\">\n\t<head>\n\t\t<link rel=\'stylesheet\' href=\'css/boot" +
                    "strap.min.css\'>\n\t\t<link rel=\'stylesheet\' href=\'css/style.min.css\'>\n\t\t<title>");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.Name));
            
            #line default
            #line hidden
            this.Write(@" Results</title>
	</head>
	<body>
		<div>
            <div class=""row container-lg lg-max-width mx-auto d-flex mt-4 mb-3 align-items-stretch"">
                <div class=""col-md-10 flex-fill text-center mx-auto m-1"">
                    <p class=""text-important mb-2 mt-1 h1"">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.YearCode));
            
            #line default
            #line hidden
            this.Write(" ");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(theEvent.Name));
            
            #line default
            #line hidden
            this.Write("</p>\n                </div>\n\t\t\t</div>\n\t\t\t<div>\n\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (distanceResults.Keys.Count < 1) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t<div class=\"container-lg lg-max-width shadow-sm p-5 mb-3 border border-ligh" +
                    "t\">\n\t\t\t\t\t\t<div class=\"text-center\">\n\t\t\t\t\t\t\t<h2>No results to display.</h2>\n\t\t\t\t\t" +
                    "\t</div>\n\t\t\t\t\t</div>\n\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } else { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t<div>\n\t\t\t\t\t\t<div class=\"row container-lg lg-max-width mx-auto d-flex align-" +
                    "items-stretch shadow-sm p-0 mb-3 border border-light\">\n\t\t\t\t\t\t\t<div class=\"p-0\">\n" +
                    "\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (distanceResults.Keys.Count > 1) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t<ul class=\"nav nav-tabs nav-fill\">\n\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 foreach (string d in distanceResults.Keys ) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t<li class=\"nav-item\" key=\"distance-");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("\">\n\t\t\t\t\t\t\t\t\t\t\t\t<a class=\"nav-link text-important h5 text-secondary\" href=\"#");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("\" role=\"button\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("</a>\n\t\t\t\t\t\t\t\t\t\t\t</li>\n\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t</ul>\n\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t<div id=\"results-parent\">\n\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 foreach (string d in distanceResults.Keys) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t<div class=\"table-responsive-sm m-3\" key=\"");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("\" id=\"");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("\">\n\t\t\t\t\t\t\t\t\t\t\t<table class=\"table table-sm\">\n\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (theEvent.EventType == Constants.Timing.EVENT_TYPE_TIME) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t<thead>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (distanceResults.Keys.Count > 1) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<tr>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"table-distance-header text-import" +
                    "ant text-center\" colSpan=\"10\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</tr>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write(@"
														<tr>
															<th class=""overflow-hidden-sm col-md text-center"">Bib</th>
															<th class=""col-sm text-center"">Place</th>
															<th class=""col-lg"">Name</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Age</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Pl</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Gender</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Pl</th>
															<th class=""col-lg text-center"">Time</th>
															<th class=""col-lg text-center""></th>
														</tr>
													</thead>
													<tbody>
														");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 foreach (TimeResult r in distanceResults[d]) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<tr key=\"");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Bib));
            
            #line default
            #line hidden
            this.Write("\">\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-sm text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Bib));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.PrettyPlaceStr));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (linkPart) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<a class=\"nav-link m-0 p-0\" href=\"part/");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Bib));
            
            #line default
            #line hidden
            this.Write("\">\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.PrettyParticipantName));
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (linkPart) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</a>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\"" +
                    ">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Age(theEvent.Date)));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Finish? r.AgePlaceStr : ""));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.PrettyGender));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Finish? r.GenderPlaceStr : ""));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Time.Substring(0, r.Time.Length > 3 ? r.Time.Length -2 : r.Time.Length)));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture((r.SegmentName == "Finish")? string.Format("Lap {0}", r.Occurrence) : r.SegmentName));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</tr>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t</tbody>\n\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } else { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t<thead>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (distanceResults.Keys.Count > 1) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<tr>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"table-distance-header text-import" +
                    "ant text-center\" colSpan=\"10\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(d));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</tr>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write(@"
														<tr>
															<th class=""overflow-hidden-sm col-md text-center"">Bib</th>
															<th class=""col-sm text-center"">Place</th>
															<th class=""col-lg"">Name</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Age</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Pl</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Gender</th>
															<th class=""overflow-hidden-lg col-sm text-center"">Pl</th>
															<th class=""overflow-hidden-lg col-lg text-center"">Chip Time*</th>
															<th class=""col-lg text-center"">Time</th>
														</tr>
													</thead>
													<tbody>
														");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 foreach (TimeResult r in distanceResults[d]) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<tr key=\"");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Bib));
            
            #line default
            #line hidden
            this.Write("\">\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-sm text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Bib));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.PrettyPlaceStr));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (linkPart) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<a class=\"nav-link m-0 p-0\" href=\"part/");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Bib));
            
            #line default
            #line hidden
            this.Write("\">\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.PrettyParticipantName));
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (linkPart) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</a>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\"" +
                    ">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Age(theEvent.Date)));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Finish? r.AgePlaceStr : ""));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.PrettyGender));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Finish? r.GenderPlaceStr : ""));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"overflow-hidden-lg text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.ChipTime.Substring(0, r.ChipTime.Length > 3 ? r.ChipTime.Length -2 : r.ChipTime.Length)));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t<td class=\"text-center\">");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(r.Finish? r.Time.Substring(0, r.Time.Length > 3 ? r.Time.Length -2 : r.Time.Length) : r.SegmentName));
            
            #line default
            #line hidden
            this.Write("</td>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t</tr>\n\t\t\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t\t\t</tbody>\n\t\t\t\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t\t\t\t</table>\n\t\t\t\t\t\t\t\t\t\t</div>\n\t\t\t\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t\t</div>\n\t\t\t\t\t\t\t</div>\n\t\t\t\t\t\t</div>\n\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 if (theEvent.EventType == Constants.Timing.EVENT_TYPE_DISTANCE) { 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t\t\t<div id=\'disclaimer\' class=\'container-lg lg-max-width shadow-sm text-cent" +
                    "er p-3 mb-3 border border-light overflow-hidden-lg\'>*Results are ranked based up" +
                    "on the Time and not the Chip Time.</div>\n\t\t\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t\t\t</div>\n\t\t\t\t");
            
            #line 1 "D:\ChronoKeep\ChronoKeepWindows\ChronoKeep\IO\HtmlTemplates\HtmlResultsTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\n\t\t\t</div>\n\t\t</div>\n\t\t<script type=\"text/javascript\" src=\'js/jquery.min.js\'></scr" +
                    "ipt>\n\t\t<script type=\"text/javascript\" src=\'js/bootstrap.min.js\'></script>\n\t</bod" +
                    "y>\n</html>");
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
        public System.Text.StringBuilder GenerationEnvironment
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
