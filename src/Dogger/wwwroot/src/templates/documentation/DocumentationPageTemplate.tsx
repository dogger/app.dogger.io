import React from 'react';
import { graphql } from 'gatsby';

import classes from './DocumentationPageTemplate.module.css';
import { Helmet } from 'react-helmet';

type DocumentationReference = {
  title: string;
  slug: string;
  depth: number;
  references: DocumentationReference[];
  parent?: DocumentationReference;
}

type DocumentationPage = {
  title: string;
  contents: string;
  references: DocumentationReference[];
}

const renderMenu = (references: DocumentationReference[]) => {
  return <>
    {references.map(x => <>
      <div className={classes.menu + " " + classes["menu" + x.depth]}>
        <a href={"#" + x.slug}>
          {x.title}
        </a>
        {renderMenu(x.references)}
      </div>
    </>)}
  </>;
}

const renderDocumentation = (post: DocumentationPage) => {
  return <div className={classes.documentationWrapper}>
    <div className={classes.documentation} dangerouslySetInnerHTML={{ __html: post.contents }} />
    <div className={classes.rootMenu}>{renderMenu(post.references)}</div>
  </div>;
}

export default ({
  data
}) => {
  const { markdownRemark } = data;
  const { frontmatter, html } = markdownRemark;
  const headings: any[] = markdownRemark.headings;
  
  const page: DocumentationPage = {
    contents: html,
    title: frontmatter.title,
    references: []
  };

  const mapHeadingToReference = (heading: any) => ({
    title: heading
      .value
      .replace("(optional)", "")
      .replace("(required)", "")
      .trim(),
    slug: heading.id,
    depth: heading.depth,
    references: []
  } as DocumentationReference);

  const getReferenceTree = () => {
    const allReferences = headings.map(mapHeadingToReference);
    const rootReferences = allReferences.filter(x => x.depth === 1);

    for(let i=1;i<allReferences.length;i++) {
      const previousReference = allReferences[i-1];
      const currentReference = allReferences[i];

      let parentReference = previousReference;
      while(currentReference.depth <= parentReference.depth) {
        parentReference = parentReference.parent;
      }
      
      currentReference.parent = parentReference;
      parentReference.references.push(currentReference);
    }

    return rootReferences;
  }

  page.references = getReferenceTree();

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