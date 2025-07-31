
namespace Codex.Integration.Tests.Lucene;

public partial record LuceneTests
{
    public static string[] AutoPrefixMergeData = [ """
     #Segment _3 terms:
     ^abs$
     ^acc
     ^acccounttags$
     ^account
     ^account$
     ^account_index$
     ^accountcreation$
     ^accountindex
     ^accountindex$
     ^accountindexoutofbounds$
     ^accountlabel
     ^accountlabel$
     ^accountlabeling$
     ^accountlabelresponse$
     ^accountresponse$
     ^accounts$
     ^accounttag
     ^accounttag$
     ^accounttaganddescription
     ^accounttaganddescription$
     ^accounttaganddescriptionresponse$
     ^accounttaganddescriptionsetting$
     ^accounttagging$
     ^accounttags
     ^accounttags$
     ^accounttagsresponse$
     ^accountuntagging$

     #Segment _0 terms:
     ^
     ^a
     ^ac
     ^account
     ^accountlabelresponse.cs$
     ^accounttaganddescriptionresponse.cs$
     ^action$

     #Segment _2 terms:
     ^a
     ^account
     ^accountresponse.cs$
     ^accounttagsresponse.cs$
     ^add


     #Segment _1 terms:
     ^b
     ^boolean$
     ^byte$
     """,

      """
     #Segment _1 terms:
     ^
     ^a
     ^addressindexparameter.cs$
     ^agorist-action/csharp-monero-rpc-client$

     #Segment _2 terms:
     ^
     ^.netstandard,version=v2.1.assemblyattributes.cs$
     ^a
     ^account

     #Segment _3 terms:
     ^
     ^.
     ^.gitignore$
     ^.netcoreapp,version=v3.1.assemblyattributes.cs$
     ^_
     ^_config.yml$

     #Segment _0 terms:
     ^
     ^a
     ^abs$
     ^add
     """,

      """
     #Segment _3 terms:
     ^
     ^_

     #Segment _0 terms:
     ^
     ^.netcoreapp,version=v3.1.assemblyattributes.cs$
     ^_config.yml$
     ^a

     #Segment _2 terms:
     ^
     ^.
     ^.gitignore$
     ^.netstandard,version=v2.1.assemblyattributes.cs$

     #Segment _1 terms:
     ^b
     ^boolean$
     ^byte$
     """

     ];
}