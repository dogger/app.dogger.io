async function createBlogPosts({ actions, graphql, reporter }) {
  const { createPage } = actions;
  const templates = {
    blog: require.resolve(`./src/templates/BlogPostTemplate.tsx`),
    documentation: require.resolve(`./src/templates/DocumentationPageTemplate.tsx`)
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

    createPage({
      path: slug,
      component: templates[type],
      context: {
        slug: slug,
      },
    });
  })
}

async function createWildcard(path, { page, actions }) {
    const { createPage } = actions
    if (page.path.indexOf("/" + path) > -1) {
      page.matchPath = "/" + path + "/*"
      createPage(page);
    }
}

exports.createPages = async (context) => {
  await createBlogPosts(context);
}

exports.onCreatePage = async (context) => {
  await createWildcard("dashboard", context);
}