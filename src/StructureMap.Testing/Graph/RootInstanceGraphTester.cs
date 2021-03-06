﻿using Shouldly;
using StructureMap.Graph;
using StructureMap.Pipeline;
using StructureMap.Testing.Widget3;
using Xunit;

namespace StructureMap.Testing.Graph
{
    public class RootInstanceGraphTester
    {
        [Fact]
        public void default_lifecycle_by_default_is_null()
        {
            var root = new RootInstanceGraph(PluginGraph.CreateRoot());
            root.DefaultLifecycleFor(typeof(IGateway)).ShouldBeNull();
        }

        [Fact]
        public void default_lifecycle_is_null_if_family_has_no_lifecycle()
        {
            var graph = PluginGraph.CreateRoot();
            graph.Families[typeof(IGateway)].SetDefault(new SmartInstance<StubbedGateway>());

            var root = new RootInstanceGraph(graph);
            root.DefaultLifecycleFor(typeof(IGateway)).ShouldBeNull();
        }

        [Fact]
        public void default_lifecycle_is_explicitly_set()
        {
            var graph = PluginGraph.CreateRoot();
            graph.Families[typeof(IGateway)].SetLifecycleTo<SingletonLifecycle>();

            var root = new RootInstanceGraph(graph);
            root.DefaultLifecycleFor(typeof(IGateway)).ShouldBeOfType<SingletonLifecycle>();
        }

        [Fact]
        public void singleton_is_just_the_plugin_graph_singletons()
        {
            var plugins = PluginGraph.CreateRoot();
            plugins.SingletonCache.ShouldNotBeNull();

            var pipeline = PipelineGraph.BuildRoot(plugins);

            pipeline.Singletons.ShouldBeTheSameAs(plugins.SingletonCache);
        }

        [Fact]
        public void transient_cache_by_default_is_a_nullo()
        {
            var plugins = PluginGraph.CreateRoot();

            var pipeline = PipelineGraph.BuildRoot(plugins);
            pipeline.Transients.ShouldBeOfType<NulloTransientCache>();
        }

        [Fact]
        public void transient_cache_of_nested_pipeline_graph_is_a_stateful_cache()
        {
            var plugins = PluginGraph.CreateRoot();

            var pipeline = PipelineGraph.BuildRoot(plugins);
            pipeline.ToNestedGraph().Transients.ShouldBeOfType<ContainerSpecificObjectCache>();
        }
    }
}