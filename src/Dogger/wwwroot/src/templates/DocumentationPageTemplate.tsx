import React from 'react';
import { graphql } from 'gatsby';

type DocumentationReference = {
  title: string;
  slug: string;
  references: DocumentationReference[];
}

type DocumentationPage = {
  title: string;
  contents: string;
  references: DocumentationReference[];
}

const renderMenu = (references: DocumentationReference[]) => {
  return <>
    {references.map(x => <>
      <div>
        <a href={"#" + x.slug}>
          {x.title}
        </a>
        {renderMenu(x.references)}
      </div>
    </>)}
  </>;
}

const renderDocumentation = (post: DocumentationPage) => {
  return <>
    <div>{renderMenu(post.references)}</div>
    <div dangerouslySetInnerHTML={{ __html: post.contents }}></div>
  </>;
}

export default ({
  data
}) => {
  const { markdownRemark } = data;
  const { frontmatter, html, headings } = markdownRemark;

  const getReferencesForLevel = (level: number) => 
    headings
      .filter(x => x.depth === level)
      .map(x => ({
        title: x.value,
        slug: x.id,
        references: getReferencesForLevel(level+1)
      } as DocumentationReference));

  const page: DocumentationPage = {
    contents: html,
    title: frontmatter.title,
    references: getReferencesForLevel(headings[0].depth)
  };
  return renderDocumentation(page);
}

export const pageQuery = graphql`
  query($slug: String!) {
    markdownRemark(frontmatter: { slug: { eq: $slug } }) {
      html
      frontmatter {
        slug
        title
      }
      headings {
        depth
        id
        value
      }
    }
  }
`