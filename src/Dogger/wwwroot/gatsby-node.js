async function createBlogPosts({ actions, graphql, reporter }) {
  const { createPage } = actions;
  const templates = {
    blog: require.resolve(`./src/templates/BlogPostTemplate.tsx`),
    documentation: require.resolve(`./src/templates/documentation/DocumentationPageTemplate.tsx`)
  };
  const result = await graphql(`
    {
      allMarkdownRemark(
        sort: { order: DESC, fields: [frontmatter___date] }
        limit: 1000
      ) {
        edges {
          node {
            frontmatter {
              slug
            }
          }
        }
      }
    }
  `);

  if (result.errors) {
    reporter.panicOnBuild(`Error while running GraphQL query.`);
    return;
  }

  result.data.allMarkdownRemark.edges.forEach(({ node }) => {
    const slug = node.frontmatter.slug;
    const type = slug.split("/")[1];

    if(!templates[type]) {
      reporter.panicOnBuild("Could not find a template for type " + type + ".");
      return;
    }

    if(slug.substr(slug.length-1, 1) === "/") {
      reporter.panicOnBuild("The slug " + slug + " ended with a slash.");
      return;
    }

    createPage({
      path: slug,
      component: templates[type],
      context: {
        slug: slug,
      },
    });
  })
}

exports.createPages = async (context) => {
  await createBlogPosts(context);
}

exports.onCreatePage = async (context) => {
}