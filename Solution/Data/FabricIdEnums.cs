namespace Fabric.Apps.WordNet.Data {

	/*================================================================================================*/
	public enum ArtifactTypeId {
		App = 1,
		User,
		Class,
		Instance,
		Url
	}

	/*================================================================================================*/
	public enum DescriptorTypeId {
		IsRelatedTo = 1,
		IsA,
		HasA,
		IsLike,
		IsNotLike,

		RefersTo,
		IsCreatedBy,
		IsInterestedIn,
		Receives,
		Consumes,
		Produces,
		ParticipatesIn,
		IsFoundIn,
		BelongsTo,

		LooksLike,
		SmellsLike,
		TastesLike,
		SoundsLike,
		FeelsLike,
		EmotesLike
	}

	/*================================================================================================*/
	public enum FactorAssertionId {
		Undefined = 1,
		Fact,
		Opinion,
		Guess
	}
	
	/*================================================================================================*/
	public enum IdentorTypeId {
		Text = 1,
		Key
	}

}