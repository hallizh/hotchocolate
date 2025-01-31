using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeExtensionTests
    {
        [Fact]
        public void ObjectTypeExtension_AddField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType<FooTypeExtension>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields.ContainsField("test"));
        }

        [Fact]
        public void ObjectTypeExtension_Infer_Field()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType<GenericFooTypeExtension>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields.ContainsField("test"));
        }

        [Fact]
        public void ObjectTypeExtension_Declare_Field()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension<FooExtension>(d =>
                {
                    d.Name("Foo");
                    d.Field(t => t.Test).Type<IntType>();
                }))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields.ContainsField("test"));
            Assert.IsType<IntType>(type.Fields["test"].Type);
        }

        [Fact]
        public async Task ObjectTypeExtension_Execute_Infer_Field()
        {
            // arrange
            // act
            IRequestExecutor executor = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType<GenericFooTypeExtension>()
                .Create()
                .MakeExecutable();

            // assert
            IExecutionResult result = await executor.ExecuteAsync("{ test }");
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeExtension_OverrideResolver()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => new ValueTask<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .Resolve(resolver)))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.Equal(resolver, type.Fields["description"].Resolver);
        }

        [Fact]
        public async Task ObjectTypeExtension_AddResolverType()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Resolver<FooResolver>())
                .Returns(new FooResolver());
            context.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field<FooResolver>(t => t.GetName2())
                    .Type<StringType>()))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            object value = await type.Fields["name2"].Resolver(context.Object);
            Assert.Equal("FooResolver.GetName2", value);
        }

        [Fact]
        public void ObjectTypeExtension_AddMiddleware()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .Use(next => context =>
                    {
                        context.Result = "BAR";
                        return default(ValueTask);
                    })))
                .Create();

            // assert
            IRequestExecutor executor = schema.MakeExecutable();
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Obsolete]
        [Fact]
        public void ObjectTypeExtension_DeprecateField_Obsolete()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => new ValueTask<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .DeprecationReason("Foo")))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["description"].IsDeprecated);
            Assert.Equal("Foo", type.Fields["description"].DeprecationReason);
        }

        [Fact]
        public void ObjectTypeExtension_DeprecateField_With_Reason()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => new ValueTask<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .Deprecated("Foo")))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["description"].IsDeprecated);
            Assert.Equal("Foo", type.Fields["description"].DeprecationReason);
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeExtension_DeprecateField_Without_Reason()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => new ValueTask<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .Deprecated()))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["description"].IsDeprecated);
            Assert.Equal(
                WellKnownDirectives.DeprecationDefaultReason,
                type.Fields["description"].DeprecationReason);
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeExtension_SetTypeContextData()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Extend()
                    .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.ContextData.ContainsKey("foo"));
        }

        [Fact]
        public void ObjectTypeExtension_SetFieldContextData()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => new ValueTask<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Extend()
                    .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["description"]
                .ContextData.ContainsKey("foo"));
        }

        [Fact]
        public void ObjectTypeExtension_SetArgumentContextData()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => new ValueTask<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Type<StringType>()
                    .Argument("a", a => a
                        .Type<StringType>()
                        .Extend()
                        .OnBeforeCreate(c => c.ContextData["foo"] = "bar"))))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["name"].Arguments["a"]
                .ContextData.ContainsKey("foo"));
        }

        [Fact]
        public void ObjectTypeExtension_SetDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy")))
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Directives.Contains("dummy"));
        }

        [Fact]
        public void ObjectTypeExtension_SetDirectiveOnField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Directive("dummy")))
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["name"]
                .Directives.Contains("dummy"));
        }

        [Fact]
        public void ObjectTypeExtension_SetDirectiveOnArgument()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a => a.Directive("dummy"))))
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["name"].Arguments["a"]
                .Directives.Contains("dummy"));
        }

        [Fact]
        public void ObjectTypeExtension_ReplaceDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Directive("dummy_arg", new ArgumentNode("a", "a"))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy_arg", new ArgumentNode("a", "b"))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            string value = type.Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_ReplaceDirectiveOnField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.Description)
                    .Directive("dummy_arg", new ArgumentNode("a", "a"))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Directive("dummy_arg", new ArgumentNode("a", "b"))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            string value = type.Fields["description"].Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_ReplaceDirectiveOnArgument()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.GetName(default))
                    .Argument("a", a => a
                        .Type<StringType>()
                        .Directive("dummy_arg", new ArgumentNode("a", "a")))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a =>
                        a.Directive("dummy_arg", new ArgumentNode("a", "b")))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            string value = type.Fields["name"].Arguments["a"]
                .Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_CopyDependencies_ToType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a => a.Directive("dummy_arg", new ArgumentNode("a", "b")))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            var value = type.Fields["name"].Arguments["a"]
                .Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_RepeatableDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Directive("dummy_rep")))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy_rep")))
                .AddDirectiveType<RepeatableDummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            int count = type.Directives["dummy_rep"].Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void ObjectTypeExtension_RepeatableDirectiveOnField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.Description)
                    .Directive("dummy_rep")))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Directive("dummy_rep")))
                .AddDirectiveType<RepeatableDummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            int count = type.Fields["description"].Directives["dummy_rep"].Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void ObjectTypeExtension_RepeatableDirectiveOnArgument()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.GetName(default))
                    .Argument("a", a => a
                        .Type<StringType>()
                        .Directive("dummy_rep", new ArgumentNode("a", "a")))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a =>
                        a.Directive("dummy_rep", new ArgumentNode("a", "b")))))
                .AddDirectiveType<RepeatableDummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            var count = type.Fields["name"].Arguments["a"]
                .Directives["dummy_rep"]
                .Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void ObjectTypeExtension_SetDirectiveOnArgument_Sdl_First()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddDocumentFromString(
                    @"extend type Foo {
                        name(a: String @dummy): String
                    }")
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["name"].Arguments["a"].Directives.Contains("dummy"));
        }

        [Fact]
        public void BindByType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>()
                .AddType<Query>()
                .AddType<Extensions>()
                .Create();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public void BindResolver_With_Property()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<BindResolver_With_Property_PersonDto>()
                .AddType<BindResolver_With_Property_PersonResolvers>()
                .Create();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public void Remove_Properties_Globally()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Remove_Properties_Globally_PersonDto>()
                .AddType<Remove_Properties_Globally_PersonResolvers>()
                .Create();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public void Remove_Fields_Globally()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Remove_Fields_Globally_PersonDto>()
                .AddType<Remove_Fields_Globally_PersonResolvers>()
                .Create();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public void Remove_Fields()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Remove_Fields_PersonDto>()
                .AddType<Remove_Fields_PersonResolvers>()
                .Create();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public void Remove_Fields_BindField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Remove_Fields_BindProperty_PersonDto>()
                .AddType<Remove_Fields_BindProperty_PersonResolvers>()
                .Create();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public void Replace_Field()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Replace_Field_PersonDto>()
                .AddType<Replace_Field_PersonResolvers>()
                .Create();

            // assert
            schema.Print().MatchSnapshot();
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Description);
            }
        }

        public class FooTypeExtension
            : ObjectTypeExtension
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Field("test")
                    .Resolver(() => new List<string>())
                    .Type<ListType<StringType>>();
            }
        }

        public class GenericFooTypeExtension
            : ObjectTypeExtension<FooExtension>
        {
            protected override void Configure(
                IObjectTypeDescriptor<FooExtension> descriptor)
            {
                descriptor.Name("Foo");
            }
        }

        public class Foo
        {
            public string Description { get; } = "hello";

            public string GetName(string a)
            {
                return null;
            }
        }

        public class FooExtension
        {
            public string Test { get; set; } = "Test123";
        }

        public class FooResolver
        {
            public string GetName2()
            {
                return "FooResolver.GetName2";
            }
        }

        public class DummyDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("dummy");
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Location(DirectiveLocation.ArgumentDefinition);
            }
        }

        public class DummyWithArgDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("dummy_arg");
                descriptor.Argument("a").Type<StringType>();
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Location(DirectiveLocation.ArgumentDefinition);
            }
        }

        public class RepeatableDummyDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("dummy_rep");
                descriptor.Repeatable();
                descriptor.Argument("a").Type<StringType>();
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Location(DirectiveLocation.ArgumentDefinition);
            }
        }

        public class Query : IMarker
        {
            public string Foo { get; } = "abc";
        }

        public class Bar : IMarker
        {
            public string Baz { get; } = "def";
        }

        [ExtendObjectType(
            // extends all types that inherit this type.
            extendsType: typeof(IMarker))]
        public class Extensions
        {
            // introduces a new field on all types that apply the parent
            public string Any([Parent] object parent)
            {
                if (parent is Query q)
                {
                    return q.Foo;
                }

                if (parent is Bar b)
                {
                    return b.Baz;
                }

                return null;
            }

            // replaces the original field baz on bar
            [GraphQLName("baz")]
            public string BazEx([Parent] Bar bar)
            {
                return bar.Baz;
            }

            // introduces a new field to query
            public Bar FooEx([Parent] Query query)
            {
                return new Bar();
            }
        }

        public interface IMarker
        {

        }

        public class BindResolver_With_Property_PersonDto
        {
            public int FriendId { get; } = 1;
        }

        [ExtendObjectType(typeof(BindResolver_With_Property_PersonDto))]
        public class BindResolver_With_Property_PersonResolvers
        {
            [BindProperty(nameof(BindResolver_With_Property_PersonDto.FriendId))]
            public List<BindResolver_With_Property_PersonDto> Friends() =>
                new List<BindResolver_With_Property_PersonDto>();
        }

        public class Remove_Properties_Globally_PersonDto
        {
            public int FriendId { get; } = 1;

            public int InternalId { get; } = 1;
        }

        [ExtendObjectType(
            typeof(Remove_Properties_Globally_PersonDto),
            IgnoreProperties = new[] { nameof(Remove_Properties_Globally_PersonDto.InternalId) })]
        public class Remove_Properties_Globally_PersonResolvers
        {
        }

        public class Remove_Fields_Globally_PersonDto
        {
            public int FriendId { get; } = 1;

            public int InternalId { get; } = 1;
        }

        [ExtendObjectType(
            typeof(Remove_Fields_Globally_PersonDto),
            IgnoreProperties = new[] { "internalId" })]
        public class Remove_Fields_Globally_PersonResolvers
        {
        }

        public class Remove_Fields_PersonDto
        {
            public int FriendId { get; } = 1;

            public int InternalId { get; } = 1;
        }

        [ExtendObjectType(typeof(Remove_Fields_PersonDto))]
        public class Remove_Fields_PersonResolvers
        {
            [GraphQLIgnore]
            public int InternalId { get; } = 1;
        }

        public class Remove_Fields_BindProperty_PersonDto
        {
            public int FriendId { get; } = 1;

            public int InternalId { get; } = 1;
        }

        [ExtendObjectType(typeof(Remove_Fields_BindProperty_PersonDto))]
        public class Remove_Fields_BindProperty_PersonResolvers
        {
            [GraphQLIgnore]
            [BindProperty(nameof(Remove_Fields_BindProperty_PersonDto.InternalId))]
            public int SomeId { get; } = 1;
        }

        public class Replace_Field_PersonDto
        {
            public int FriendId { get; } = 1;

            public int InternalId { get; } = 1;
        }

        [ExtendObjectType(typeof(Replace_Field_PersonDto))]
        public class Replace_Field_PersonResolvers
        {
            [BindProperty(nameof(Replace_Field_PersonDto.InternalId))]
            public string SomeId { get; } = "abc";
        }
    }
}
