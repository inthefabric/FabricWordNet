using System;
using Fabric.Apps.WordNet.Data;
using Fabric.Apps.WordNet.Data.Domain;
using Fabric.Apps.WordNet.Structures;
using NHibernate;

namespace Fabric.Apps.WordNet.Factors {

	/*================================================================================================*/
	public class CustomFactors {

		public const int PartOfSpeechWordId = 124534; //[null, 124534]
		public const int NounWordId = 163388; //[87361, 163388]
		public const int VerbWordId = 196317; //[110296, 196317]
		public const int AdjectiveWordId = 1735; //[711, 1735]
		public const int AdverbWordId = 54402; //[29942, 54402]

		public const int SynsetWordId = 190658; //[106016, 190658]
		public const int WordNet31WordId = 198790; //[112198, 198790]

		private readonly ArtifactSet vArtSet;
		private readonly SessionProvider vSessProv;
		private DateTime vStartTime;


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		public CustomFactors(ArtifactSet pArtSet) {
			vArtSet = pArtSet;
			vSessProv = new SessionProvider();
		}

		/*--------------------------------------------------------------------------------------------*/
		public void Start() {
			vStartTime = DateTime.UtcNow;

			using ( ISession sess = vSessProv.OpenSession() ) {
				Console.WriteLine("");
				Console.WriteLine("Starting Custom Factors..."+TimerString());
				Console.WriteLine("");

				InsertPartOfSpeechFactors(sess);
				InsertSynsetFactors(sess);
			}
		}

		/*--------------------------------------------------------------------------------------------*/
		private string TimerString() {
			return " (CustomFactor Time: "+(DateTime.UtcNow-vStartTime).TotalSeconds+")";
		}


		////////////////////////////////////////////////////////////////////////////////////////////////
		/*--------------------------------------------------------------------------------------------*/
		private void InsertPartOfSpeechFactors(ISession pSess) {
			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Part-of-Speech Factors...");

				foreach ( Artifact art in vArtSet.List ) {
					Synset ss = pSess.Get<Synset>(art.Synset == null ? 
						vArtSet.WordIdToSynsetIdMap[art.Word.Id] : art.Synset.Id);
					Artifact targArt;

					switch ( ss.PartOfSpeechId ) {
						case (int)WordNetEngine.POS.Noun:
							targArt = vArtSet.WordIdMap[NounWordId];
							break;

						case (int)WordNetEngine.POS.Verb:
							targArt = vArtSet.WordIdMap[VerbWordId];
							break;

						case (int)WordNetEngine.POS.Adjective:
							targArt = vArtSet.WordIdMap[AdjectiveWordId];
							break;

						case (int)WordNetEngine.POS.Adverb:
							targArt = vArtSet.WordIdMap[AdverbWordId];
							break;

						default:
							Console.WriteLine("Unknown POS: "+
								art.Id+" / "+art.Name+" / "+ss.PartOfSpeechId);
							continue;
					}

					var f = new Factor();
					f.PrimaryArtifact = art;
					f.RelatedArtifact = targArt;
					f.IsDefining = true;
					f.DescriptorTypeId = (byte)DescriptorTypeId.IsAnInstanceOf;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+art.Name+"]  "+DescriptorTypeId.IsAnInstanceOf+"  ["+targArt.Name+"]";
					f.PrimaryClassRefine = vArtSet.WordIdMap[PartOfSpeechWordId];
					pSess.Save(f);
				}

				Console.WriteLine("Comitting Factors..."+TimerString());
				tx.Commit();
				Console.WriteLine("Finished Factors"+TimerString());
				Console.WriteLine("");
			}
		}
		
		/*--------------------------------------------------------------------------------------------*/
		private void InsertSynsetFactors(ISession pSess) {
			using ( ITransaction tx = pSess.BeginTransaction() ) {
				Console.WriteLine("Building Synset Factors...");

				foreach ( Artifact art in vArtSet.List ) {
					if ( art.Synset == null ) {
						continue;
					}

					Synset ss = pSess.Get<Synset>(art.Synset.Id);
					Artifact targArt = vArtSet.WordIdMap[WordNet31WordId];

					var f = new Factor();
					f.PrimaryArtifact = art;
					f.RelatedArtifact = targArt;
					f.IsDefining = false;
					f.DescriptorTypeId = (byte)DescriptorTypeId.RefersTo;
					f.AssertionId = (byte)FactorAssertionId.Fact;
					f.Note = "["+art.Name+"]  "+DescriptorTypeId.RefersTo+"  ["+targArt.Name+"]";
					f.RelatedClassRefine = vArtSet.WordIdMap[SynsetWordId];
					f.IdentorTypeId = (int)IdentorTypeId.Key;
					f.IdentorValue = ss.SsId;
					pSess.Save(f);
				}

				Console.WriteLine("Comitting Factors..."+TimerString());
				tx.Commit();
				Console.WriteLine("Finished Factors"+TimerString());
				Console.WriteLine("");
			}
		}

	}

}