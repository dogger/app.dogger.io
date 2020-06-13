import { graphql } from 'gatsby'

import './blog.css';


export type DocumentationReference = {
    title: string;
    slug: string;
    references: DocumentationReference[];
}

export type DocumentationPage = {
    title: string;
    contents: string;
    references: DocumentationReference[];
}

export const renderDocumentation = (post: DocumentationPage) => {
    return null;
}

export default () => {
    return renderDocumentation(null);
};

export const pageQuery = graphql`
query AllDocumentationQuery {
    allMarkdownRemark {
        edges {
            node {
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
    }
}  
`