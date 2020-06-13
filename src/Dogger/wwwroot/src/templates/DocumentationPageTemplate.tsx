import React from 'react';
import { graphql } from 'gatsby';

export default () => {
    return null;
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