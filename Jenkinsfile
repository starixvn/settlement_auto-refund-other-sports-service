@Library("JenkinsPipelineLibrary") _
pipeline {
  agent any

  options{
    buildDiscarder(logRotator(numToKeepStr: '5'))    
    disableConcurrentBuilds()
  }

  triggers{
    pollSCM("H/15 * * * *")
  } 

  stages {
    stage('Build') {
      steps {        
        script{
          pipelineLib.beginSonarQubeForMsBuild("AutoRefund-OtherSports-Service", "Olympus /  AutoRefund OtherSports Service", "/d:sonar.cs.opencover.reportsPaths=%WORKSPACE%\\CoverageReports\\*.xml /d:sonar.exclusions=**/*.native.*,**/*Criteria.*,**/Models/*.cs,**/Constants/*.cs /d:sonar.cs.vstest.reportsPaths=%WORKSPACE%\\TestResults\\*.trx")

          pipelineLib.msBuild16("Fanex.AutoRefundOtherSports.Service.sln")
        }
      }
    }
   
     stage("Test"){
      parallel{
        stage("C# Unit Test"){
          steps{
            script{
              pipelineLib.xUnitForNet5()
            }
          }
        }
      }      
    }    

    stage("SonarQube Analysis"){
      steps{       
        script{
          pipelineLib.endSonarQubeForMsBuild()
        }
      }
      
      post{
        always{
          script{
            if(manager.logContains(".*Quality gate status.*")){              
              pipelineLib.generateSonarQubeReport("AutoRefund-OtherSports-Service")
            }
          }
        }
      }
    }
  }

  post{
    always{
      archiveArtifacts "*.xml,*.email"
      step([$class: 'ACIPluginPublisher', name: '*.xml', shownOnProjectPage: false])
      mstest failOnError: false
    }
    unsuccessful{
      emailext body: '$DEFAULT_CONTENT', subject: '$DEFAULT_SUBJECT', to: 'kaden.trinh@starixvn.com;ryan.ngo@starixvn.com;levi.vu@starixvn.com'
    }
  }
}
