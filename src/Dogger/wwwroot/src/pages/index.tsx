import React, { ReactFragment } from 'react';
import { graphql } from "gatsby";
import { useTheme, Typography, Container, Box, Card, CardContent, Link, Divider, Grid, makeStyles, Theme, useMediaQuery } from '@material-ui/core';
import { GitHub, Storage, ArrowDownward, AssignmentTurnedIn, MoneyOff, Share } from '@material-ui/icons';
import { PullDogPricingTable } from '../components/pull-dog/PullDogPricingTable';
import Img from "gatsby-image";
import { Helmet } from 'react-helmet';

const useStyles = makeStyles({
    header: () => ({}),
    accentColor: (existingTheme: Theme) => ({
        color: existingTheme.palette.type === "dark" ?
            'white' :
            existingTheme.palette.primary.main
    })
});

const LandingPageSection = (props: { children, style?}) =>
    <section style={{
        ...props.style,
        position: 'relative'
    }}>
        {props.children}
    </section>;

const BenefitGridItem = (props: { icon: JSX.Element, title: string, description: ReactFragment }) => {
    return <>
        <div style={{
            display: 'flex',
            flexDirection: 'column',
            padding: 48
        }}>
            <div style={{
                alignSelf: 'center'
            }}>
                {props.icon}
            </div>
            <Typography component="h4" style={{
                fontSize: '24px',
                textAlign: 'center',
                padding: 16,
                paddingTop: 24
            }}>
                {props.title}
            </Typography>
            <Typography variant="body1" component="p" style={{
                opacity: 0.75,
                fontSize: '18px',
                textAlign: 'center'
            }}>
                {props.description}
            </Typography>
        </div>
    </>;
}

export const Timeline = (props: { entries: Array<{ title: string, text: JSX.Element, image: any }> }) => {
    const theme = useTheme();
    const isDownFromMedium = useMediaQuery(theme.breakpoints.down('sm'));

    const isDarkTheme = theme.palette.type === "dark";
    const linkColor = isDarkTheme ?
        "rgba(0,0,0,0.25)" :
        "rgba(0,0,0,0.08)";

    let isLeft = true;

    return <div style={{
        display: 'flex',
        flexDirection: 'column'
    }}>
        {props
            .entries
            .map((entry, index) => {
                const elements = new Array<JSX.Element>();

                const elementsVerticalMargin = 48;
                const elementsHorizontalMargin = 8;

                const bulletSize = 20;

                const linkWidth = 20;
                const linkMargin = 10;
                const linkBorderWidth = 2;

                const elementWidth = `${isDownFromMedium ? 100 : 50}vw - ${linkWidth * 2}px - ${linkMargin * (isDownFromMedium ? 1 : 2)}px - ${elementsHorizontalMargin}px`;
                const maxElementWidth = 450;

                const calc = (inner: string) => `calc(${inner})`;

                const linkElement = <div style={{
                    height: 1,
                    borderTop: `${linkBorderWidth}px dashed ${linkColor}`,
                    width: linkWidth,
                    alignSelf: 'flex-start',
                    margin: linkMargin,
                    marginTop: bulletSize / 2
                }} />

                const imageOffset = isDownFromMedium ? -30 : 50;
                const imageElement =
                    <Img alt="Timeline screenshot" fluid={entry.image} style={{
                        width: calc(elementWidth),
                        maxWidth: maxElementWidth,
                        borderRadius: 10,
                        marginTop: -imageOffset,
                        marginLeft: 0,
                        marginRight: 0,
                        boxShadow: '0px 0px 28px 6px rgba(0,0,0,0.05)',
                        borderWidth: 1,
                        borderColor: 'rgba(0,0,0,0.05)',
                        borderStyle: 'solid',
                        objectFit: 'contain',
                        alignSelf: 'flex-start'
                    }} />;

                const contentElement =
                    <div style={{
                        width: calc(elementWidth),
                        maxWidth: maxElementWidth,
                        textAlign: isLeft ? 'right' : 'left',
                        marginTop: -12
                    }}>
                        <div style={{
                            fontSize: 40
                        }}>{entry.title}</div>
                        <div style={{
                            marginTop: 8,
                            opacity: 0.75,
                            fontSize: 16
                        }}>{entry.text}</div>
                        {isDownFromMedium && imageElement}
                    </div>;

                const bulletElement =
                    <>
                        <div style={{
                            position: 'relative'
                        }}>
                            <Box boxShadow={2} style={{
                                backgroundColor: theme.palette.primary.main,
                                width: bulletSize,
                                height: bulletSize,
                                borderRadius: bulletSize * 2,
                                alignSelf: 'flex-start'
                            }} />
                            {index !== props.entries.length - 1 && <div style={{
                                borderRight: `2px solid ${linkColor}`,
                                position: 'absolute',
                                top: bulletSize + linkMargin,
                                left: bulletSize / 2 - 1,
                                bottom: -elementsVerticalMargin * 2 + linkMargin
                            }} />}
                        </div>
                    </>;

                elements.push(...(isLeft ?
                    [contentElement, linkElement] :
                    [!isDownFromMedium && imageElement, linkElement]).filter(x => !!x));

                elements.push(bulletElement);

                elements.push(...(!isLeft ?
                    [linkElement, contentElement] :
                    [!isDownFromMedium && linkElement, !isDownFromMedium && imageElement]).filter(x => !!x));

                if (!isDownFromMedium)
                    isLeft = !isLeft;

                return <div key={`timeline-entry-${index}`} style={{
                    display: 'flex',
                    flexDirection: 'row',
                    justifyContent: 'center',
                    marginTop: elementsVerticalMargin,
                    marginBottom: elementsVerticalMargin,
                    marginLeft: elementsHorizontalMargin,
                    marginRight: elementsHorizontalMargin
                }}>
                    {elements}
                </div>;
            })}
    </div>;
}

const ProductCard = (props: {
    description: string,
    title: string,
    icon: JSX.Element,
    disabled?: boolean,
    anchor: string
}) => {
    const theme = useTheme();
    const styles = useStyles(theme);

    return <Card style={{
        maxWidth: 350,
        width: '100%',
        margin: 24,
        opacity: props.disabled ? 0.5 : 1
    }}>
        <CardContent style={{
            padding: 48,
            display: 'flex',
            flexDirection: 'column',
            height: '100%'
        }}>
            <div style={{ textAlign: 'center' }}>
                {props.icon}
            </div>
            <Typography component="h1" style={{
                fontSize: '30px',
                textAlign: 'center',
                padding: 16,
                paddingTop: 24
            }}>
                {props.title}
            </Typography>
            <Typography variant="body1" component="p" style={{
                opacity: 0.75,
                fontSize: '18px'
            }}>
                {props.description}
            </Typography>
            <div style={{ flexGrow: 1 }} />
            <Link href={props.disabled ? null : "#" + props.anchor} className={styles.accentColor} style={{
                fontSize: '20px',
                fontWeight: 'bold',
                paddingTop: 32,
                display: 'flex',
                alignSelf: 'center'
            }}>
                {props.disabled ?
                    "Coming soon" :
                    "Find out more"}
            </Link>
        </CardContent>
    </Card>;
}

const App = ({data}) => {
    const theme = useTheme();
    const styles = useStyles(theme);
    const isDownFromMedium = useMediaQuery(theme.breakpoints.down('sm'));

    return <>
        <Helmet>
            <meta name="description" content="Dogger has services and products to aid you in your Docker-related workflow." />
        </Helmet>
        <LandingPageSection style={{
            paddingTop: '6rem',
            paddingBottom: '20rem',
            height: '800px'
        }}>
            <div style={{
                background: 'linear-gradient(-30deg, #86613d 50%, #573715 100%)',
                position: 'absolute',
                top: 0,
                bottom: 0,
                left: 0,
                right: 0,
                zIndex: -2
            }}>
            </div>
            <img
                alt="Shape divider"
                src={theme.palette.type === "dark" ?
                    "/images/landing/hero-svg-shape-5-dark.svg" :
                    "/images/landing/hero-svg-shape-5-light.svg"}
                style={{
                    position: 'absolute',
                    top: 'auto',
                    borderWidth: 0,
                    padding: 0,
                    margin: 0,
                    right: 0,
                    left: 0,
                    bottom: -1,
                    zIndex: -1
                }} />
            <Container style={{
                maxWidth: '1140px'
            }}>
                <Grid container spacing={0}>
                    <Grid sm={12} md={5} item style={{
                        alignSelf: 'center'
                    }}>
                        <Typography component="h1" style={{
                            fontSize: '40px',
                            color: 'white'
                        }}>
                            Docker all the things
                        </Typography>
                        <Typography variant="body1" component="p" style={{
                            opacity: 0.5,
                            fontSize: '20px',
                            color: 'white',
                            paddingTop: 8,
                            paddingBottom: 16
                        }}>
                            At Dogger, we make tools and services that enable you to do more with Docker as a developer.
                        </Typography>
                        <Typography variant="body1" component="p" style={{
                            textTransform: 'uppercase',
                            color: 'white'
                        }}>
                            <ArrowDownward style={{
                                fontSize: 20,
                                position: 'relative',
                                top: 4
                            }} />
                            <Link href="#products" color="inherit" style={{ paddingLeft: 8 }}>Scroll down to learn more</Link>
                        </Typography>
                    </Grid>
                    <Grid sm={12} md={7} item style={{
                        width: '100%',
                        padding: 32,
                        display: 'flex',
                        justifyContent: 'center'
                    }}>
                        <img alt="Dogger logo" src="/images/dogger-no-title.svg" style={{
                            width: isDownFromMedium ? 450 : '100%',
                            alignSelf: 'center',
                            objectFit: 'cover'
                        }} />
                    </Grid>
                </Grid>
            </Container>
        </LandingPageSection>
        <LandingPageSection>
            <Container style={{
                maxWidth: '1140px',
                display: 'flex',
                flexDirection: 'column'
            }}>
                <Typography id="products" component="h2" className={styles.header} style={{
                    fontSize: '50px',
                    textAlign: 'center',
                    marginTop: -100,
                    paddingTop: 100
                }}>
                    Our two products
                </Typography>
                <Typography variant="body1" component="p" style={{
                    opacity: 0.5,
                    fontSize: '20px',
                    textAlign: 'center',
                    paddingTop: 8,
                    paddingBottom: 48
                }}>
                    Designed to complement each other, but not mutually exclusive.
                </Typography>
                <Grid container style={{ width: 'auto', justifyContent: 'center' }}>
                    <Grid item sm={12} md={6} style={{
                        flexBasis: 'unset',
                        display: 'flex',
                        justifyContent: 'stretch'
                    }}>
                        <ProductCard
                            anchor="pull-dog"
                            title="Pull Dog"
                            description="Automated test environments from your docker-compose.yml file for every pull request you open."
                            icon={<GitHub className={styles.accentColor} style={{
                                fontSize: 100
                            }} />} />
                    </Grid>
                    <Grid item sm={12} md={6} style={{
                        flexBasis: 'unset',
                        display: 'flex',
                        justifyContent: 'stretch'
                    }}>
                        <ProductCard
                            anchor="hosting"
                            title="Hosting"
                            description="Docker Compose in the cloud, using a simple but powerful CLI."
                            disabled
                            icon={<Storage className={styles.accentColor} style={{
                                fontSize: 100
                            }} />} />
                    </Grid>
                </Grid>
            </Container>
        </LandingPageSection>
        <Divider style={{ margin: 96 }} />
        <LandingPageSection>
            <>
                <Container style={{
                    maxWidth: '1140px',
                    display: 'flex',
                    flexDirection: 'column'
                }}>
                    <Typography id="pull-dog" component="h3" className={styles.header} style={{
                        fontSize: '60px',
                        alignSelf: 'center',
                        marginTop: -100,
                        paddingTop: 100
                    }}>
                        Pull Dog
                    </Typography>
                    <Typography variant="body1" component="div" style={{
                        opacity: 0.5,
                        fontSize: '20px',
                        alignSelf: 'center',
                        textAlign: 'center',
                        display: 'block',
                        paddingTop: 8,
                        paddingBottom: 100,
                        maxWidth: 530
                    }}>
                        The simplest way to explain Pull Dog is by showing the typical developer flow as a timeline with screenshots.
                    </Typography>
                </Container>
            </>
            <>
                <Timeline entries={[
                    {
                        title: "Pull request opened",
                        text: <>
                            This causes Pull Dog to start creating a brand new test environment for the feature you're about to merge in.<br /><br />
                            The server is created with a docker-compose file from your repository, that you specify.
                        </>,
                        image: data.pullRequestOpenedImage.childImageSharp.fluid
                    },
                    {
                        title: "Environment ready",
                        text: <>
                            HTTP and HTTPS links are provided along with the connection details for the server.
                            <br /><br />
                            The ports specified are the exposed ports of your docker-compose file, and are the only ones open in the firewall.
                        </>,
                        image: data.environmentReadyImage.childImageSharp.fluid
                    },
                    {
                        title: "Pull request closed",
                        text: <>
                            The test environment is destroyed again, along with all of its files and variables that were attached to it.
                        </>,
                        image: data.pullRequestClosedImage.childImageSharp.fluid
                    }
                ]} />
            </>
            <Container style={{
                maxWidth: '1140px',
                marginTop: 128,
                marginBottom: 128
            }}>
                <Grid container spacing={0}>
                    <Grid item xs={12} sm={6} md={4}>
                        <BenefitGridItem
                            title="Better quality"
                            description={<>Don't just review code. Review the UX and test for bugs against a real environment before merging.</>}
                            icon={<AssignmentTurnedIn className={styles.accentColor} style={{ fontSize: 80 }} />}
                        />
                    </Grid>
                    <Grid item xs={12} sm={6} md={4}>
                        <BenefitGridItem
                            title="Starts free"
                            description={<>There's a free plan available, and we <a target="_blank" href="/blog/pull-dog-is-practically-non-profit">earn less than $1 a month</a> on the paid plans.</>}
                            icon={<MoneyOff className={styles.accentColor} style={{ fontSize: 80 }} />}
                        />
                    </Grid>
                    <Grid item xs={12} sm={6} md={4}>
                        <BenefitGridItem
                            title="Share your feature"
                            description={<>Pull Dog lets you show off a temporary feature in a PR you're working on to a friend or colleague.</>}
                            icon={<Share className={styles.accentColor} style={{ fontSize: 80 }} />}
                        />
                    </Grid>
                    <Grid item xs={12} sm={6} md={4}>
                        <BenefitGridItem
                            title="Fully open source"
                            description={<>We love open source. Everything we make at Dogger is <a rel="nofollow noopener noreferrer" target="_blank" href="https://github.com/dogger">fully open source</a>.</>}
                            icon={<GitHub className={styles.accentColor} style={{ fontSize: 80 }} />}
                        />
                    </Grid>
                </Grid>
            </Container>
            <Container style={{
                maxWidth: '1140px',
                display: 'flex',
                flexDirection: 'column',
                paddingLeft: 16,
                paddingRight: 16
            }}>
                <PullDogPricingTable />
                <Typography style={{ fontStyle: 'italic', opacity: 0.5, fontSize: 14, padding: 24, paddingBottom: 8 }}>
                    * The test environment pool is what determines how many simultaneous test environments that can be open at a given point in time. If you want test environments for 5 open pull requests at the same time, you need an environment pool of at least 5.
                </Typography>
                <Typography style={{ fontStyle: 'italic', opacity: 0.5, fontSize: 14, padding: 24, paddingTop: 8 }}>
                    ** The global test environment pool has a varying size depending on demand and how much spare capacity we have laying around. This means that if many are using Pull Dog's demo environments, there won't be a slot available for you at the time of opening a pull request. This is also why we can't guarantee a long lifespan for these servers - they may get recycled after 15 minutes, if someone else is in need of them.
                </Typography>
            </Container>
        </LandingPageSection>
    </>;
}

export default ({data}) => {
    return (
        <App data={data} />
    );
}

export const query = graphql`
  query {
    pullRequestOpenedImage: file(
      relativePath: { eq: "pull-dog/screenshot-1.jpg" }
    ) {
      childImageSharp {
        fluid(maxWidth: 450) {
          ...GatsbyImageSharpFluid_withWebp
        }
      }
    }
    environmentReadyImage: file(
      relativePath: { eq: "pull-dog/screenshot-2.jpg" }
    ) {
      childImageSharp {
        fluid(maxWidth: 450) {
          ...GatsbyImageSharpFluid_withWebp
        }
      }
    }
    pullRequestClosedImage: file(
      relativePath: { eq: "pull-dog/screenshot-3.jpg" }
    ) {
      childImageSharp {
        fluid(maxWidth: 450) {
          ...GatsbyImageSharpFluid_withWebp
        }
      }
    }
  }
`