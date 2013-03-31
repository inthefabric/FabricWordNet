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
		IsAnInstanceOf,
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
		Requires,

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

	/*================================================================================================*/
	public enum DirectorActionId {
		Read = 1,
		Listen,
		View,
		Consume,
		Perform,
		Produce,
		Destroy,
		Modify,
		Obtain,
		Locate,
		Travel,
		Become,
		Explain,
		Give,
		Learn,
		Start,
		Stop
	}

	/*================================================================================================*/
	public enum DirectorTypeId {
		Hyperlink = 1,
		DefinedPath,
		SuggestPath,
		AvoidPath,
		Causality
	}

}