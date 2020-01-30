import React from "react"
import PropTypes from "prop-types"

export default function HTML(props) {
  return (
    <html {...props.htmlAttributes}>
      <head>
        <meta charSet="utf-8" />
        <meta httpEquiv="x-ua-compatible" content="ie=edge" />
        <link rel="apple-touch-icon" sizes="180x180" href="https://dogger.io/apple-touch-icon.png" />
        <link rel="icon" type="image/png" sizes="32x32" href="https://dogger.io/favicon-32x32.png" />
        <link rel="icon" type="image/png" sizes="16x16" href="https://dogger.io/favicon-16x16.png" />
        <link rel="icon" href="https://dogger.io/favicon.ico" />
        <link rel="apple-touch-icon" href="https://dogger.io/logo192.png" />
        <script async defer data-domain="dogger.io" src="https://plausible.io/js/plausible.js"></script>
        <meta
          name="viewport"
          content="width=device-width, initial-scale=1"
        />
        {props.headComponents}
      </head>
      <body {...props.bodyAttributes}>
        {props.preBodyComponents}
        <div
          key={`body`}
          id="___gatsby"
          dangerouslySetInnerHTML={{ __html: props.body }}
        />
        {props.postBodyComponents}
      </body>
    </html>
  )
}

HTML.propTypes = {
  htmlAttributes: PropTypes.object,
  headComponents: PropTypes.array,
  bodyAttributes: PropTypes.object,
  preBodyComponents: PropTypes.array,
  body: PropTypes.string,
  postBodyComponents: PropTypes.array,
}
