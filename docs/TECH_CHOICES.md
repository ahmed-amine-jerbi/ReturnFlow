# Why this stack

Researched on 2026-10-21 for the US, European and Tunisian markets, looking ahead to 2027 and beyond.

## What the sources say

| Market | Signals found |
|---|---|
| **Global** | TypeScript became GitHub's most-used language in August 2025, passing Python and JavaScript ([Octoverse figures as summarized here](https://quashbugs.com/blog/programming-language-popularity-statistics)). React is the most widely used web framework. Python is growing fastest, driven by AI work. |
| **United States** | In an analysis of about 122,000 software engineer postings: Python in 39%, JavaScript 36%, TypeScript 30%, Java 29%, with Docker, Kubernetes and AWS each around 15% ([Huntr](https://huntr.co/resume-skills/software-engineer)). React plus TypeScript is described as one of the most frequent skill pairs ([Slategit](https://slategit.com/blog/most-in-demand-programming-languages-2026)). |
| **Europe** | Python has the highest raw posting volume in 2026, and Java has the most consistent density in Germany and the Netherlands ([NextLevelJobs](https://nextleveljobs.eu/blog/7-in-demand-programming-languages-for-2026)). Java is strongest in Germany, Python and Java lead in France ([LangPop](https://langpop.com/blog/programming-language-trends-by-region)). |
| **Tunisia** | A ministry study of ICT skills lists software development, web application development, cloud, data engineering and cybersecurity as the most in-demand occupations ([allAfrica](https://allafrica.com/stories/202607070537.html)). Job guides name JavaScript, React, Angular and Flutter for web and mobile, Docker, Kubernetes and AWS for DevOps, and Python for data ([Africarrieres](https://africarrieres.com/tunisia/en/guide/marche-emploi/in-demand-profiles)). Startup listings mention .NET, React and SQL Server, often with PFE internships converting to jobs ([Nucamp](https://www.nucamp.co/blog/top-10-tech-startups-hiring-junior-developers-in-tunisia-in-2026)). |

## Decisions

| Layer | Choice | Reason |
|---|---|---|
| Web and mobile UI | **TypeScript + React** | The strongest overlap across all three markets, and one codebase serves web, phone and later a packaged mobile app. |
| AI and data | **Python** | Highest or near-highest demand in the US and Europe, growing in Tunisia, and the natural home of scikit-learn and the AI ecosystem. |
| Business API | **C# / .NET** | Enterprise stack seen in Tunisian startup listings, and it fits a business information systems profile. |
| Identity (next project) | **Java / Spring Boot** | Java stays strong in European enterprise and Tunisian companies, and Keycloak is a Java project. |
| Database | **PostgreSQL + SQL** | SQL appears in nearly every posting, and PostgreSQL leads developer surveys. |
| Delivery | **Docker, GitHub Actions** | Docker and CI appear in a large share of postings. |

## Limits of this research

- Most sources are aggregators and blogs, not primary datasets. Treat percentages as rough.
- Tunisian data is thin and partly comes from bootcamp blogs. Check live listings on Keejob, Tanitjobs and LinkedIn before you decide where to specialize.
- Figures attributed to the Stack Overflow survey differ between sources (some cite the 2025 edition as the latest), so this document avoids quoting exact survey numbers.
- Job postings lag technology by 18 to 36 months ([LangPop](https://langpop.com/blog/programming-language-job-demand-2026)). Anything about 2027 and later is an extrapolation of current trends, not a measurement.
