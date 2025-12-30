package statetree

type StateTreeBuilder struct {
	root *StateTree
}

func Create() *StateTreeBuilder {
	return &StateTreeBuilder{
		root: New(),
	}
}

func (stb *StateTreeBuilder) Key(key string) *StateTreeBuilder {
	stb.root.Key = key
	return stb
}

func (stb *StateTreeBuilder) Leaf(key string, fn ContextFunc) *StateTreeBuilder {
	if stb.root.Select == nil {
		stb.root.Select = make(map[interface{}]*StateTree)
	}

	leaf := New()
	leaf.ContextFunc = fn
	stb.root.Select[key] = leaf

	return stb
}

func (stb *StateTreeBuilder) LeafJson(key string, fn JsonFunc) *StateTreeBuilder {
	if stb.root.Select == nil {
		stb.root.Select = make(map[interface{}]*StateTree)
	}

	leaf := New()
	leaf.JsonFunc = fn
	stb.root.Select[key] = leaf

	return stb
}

func (stb *StateTreeBuilder) DefaultLeaf(fn ContextFunc) *StateTreeBuilder {
	return stb.Leaf(DefaultKey, fn)
}

func (stb *StateTreeBuilder) DefaultLeafJson(fn JsonFunc) *StateTreeBuilder {
	return stb.LeafJson(DefaultKey, fn)
}

func (stb *StateTreeBuilder) Optional(key string, fn ContextFunc) *StateTreeBuilder {
	if stb.root.OptionalParams == nil {
		stb.root.OptionalParams = make(map[string]bool)
	}

	stb.root.OptionalParams[key] = true
	return stb.Leaf(key, fn)
}

func (stb *StateTreeBuilder) OptionalJson(key string, fn JsonFunc) *StateTreeBuilder {
	if stb.root.OptionalParams == nil {
		stb.root.OptionalParams = make(map[string]bool)
	}

	stb.root.OptionalParams[key] = true
	return stb.LeafJson(key, fn)
}

func (stb *StateTreeBuilder) Build() *StateTree {
	return stb.root
}

func (stb *StateTreeBuilder) Branch(key string, builder *StateTreeBuilder) *StateTreeBuilder {
	if stb.root.Select == nil {
		stb.root.Select = make(map[interface{}]*StateTree)
	}

	stb.root.Select[key] = builder.Build()
	return stb
}

func (stb *StateTreeBuilder) DefaultBranch(builder *StateTreeBuilder) *StateTreeBuilder {
	return stb.Branch(DefaultKey, builder)
}
