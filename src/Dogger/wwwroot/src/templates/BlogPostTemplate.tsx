import React from "react"
import { graphql } from "gatsby"
import {renderBlogPost, BlogPost, BlogPage, generateLinkFromTitle} from '../pages/blog';
import { Helmet } from "react-helmet";
import moment from "moment";

export default function Template({
  data
}) {
  const { markdownRemark } = data;
  const { frontmatter, html } = markdownRemark;

  const post: BlogPost = {
    contents: html,
    time: moment(frontmatter.date),
    title: frontmatter.title,
    summary: frontmatter.summary
  };

  return <BlogPage>
    <>
    <Helmet>
        <title>{post.title}</title>
        <meta 
            name="description" 
            content={post.summary} />
        <link rel="canonical" href={`https://dogger.io${generateLinkFromTitle(post.title)}`} />
    </Helmet>
    <article>
        {renderBlogPost(post)}
    </article>
    </>
  </BlogPage>
};

export const pageQuery = graphql`
  query($slug: String!) {
    markdownRemark(frontmatter: { slug: { eq: $slug } }) {
      html
      frontmatter {
        date
        slug
        title
        summary
      }
    }
  }
`