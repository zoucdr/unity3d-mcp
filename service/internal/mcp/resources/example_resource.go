package resources

type ExampleResource struct{}

func (er *ExampleResource) Url() string {
	return "file://example.txt"
}

func (er *ExampleResource) Name() string {
	return "Example Resource"
}

func (er *ExampleResource) Description() string {
	return "An example resource demonstrating the MCP framework"
}

func (er *ExampleResource) MimeType() string {
	return "text/plain"
}
